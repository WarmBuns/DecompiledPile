using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using HG;
using RoR2.Items;
using RoR2.Stats;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class DotController : NetworkBehaviour
{
	public enum DotIndex
	{
		None = -1,
		Bleed,
		Burn,
		Helfire,
		PercentBurn,
		Poison,
		Blight,
		SuperBleed,
		StrongerBurn,
		Fracture,
		LunarRuin,
		Count
	}

	public class DotDef
	{
		public float interval;

		public float damageCoefficient;

		public DamageColorIndex damageColorIndex;

		public BuffDef associatedBuff;

		public BuffDef terminalTimedBuff;

		public float terminalTimedBuffDuration;

		public bool resetTimerOnAdd;
	}

	private class DotStack
	{
		public DotIndex dotIndex;

		public DotDef dotDef;

		public GameObject attackerObject;

		public TeamIndex attackerTeam;

		public float timer;

		public float damage;

		public DamageTypeCombo damageType;

		public void Reset()
		{
			dotIndex = DotIndex.Bleed;
			dotDef = null;
			attackerObject = null;
			attackerTeam = TeamIndex.Neutral;
			timer = 0f;
			damage = 0f;
			damageType = default(DamageTypeCombo);
		}
	}

	private sealed class DotStackPool : BasePool<DotStack>
	{
		protected override void ResetItem(DotStack item)
		{
			item.Reset();
		}
	}

	private class PendingDamage
	{
		public GameObject attackerObject;

		public float totalDamage;

		public DamageTypeCombo damageType;

		public void Reset()
		{
			attackerObject = null;
			totalDamage = 0f;
			damageType = default(DamageTypeCombo);
		}
	}

	private sealed class PendingDamagePool : BasePool<PendingDamage>
	{
		protected override void ResetItem(PendingDamage item)
		{
			item.Reset();
		}
	}

	public delegate void OnDotInflictedServerGlobalDelegate(DotController dotController, ref InflictDotInfo inflictDotInfo);

	public const float minDamagePerTick = 1f;

	private static DotDef[] dotDefs;

	private static readonly Dictionary<int, DotController> dotControllerLocator = new Dictionary<int, DotController>();

	private static readonly List<DotController> instancesList = new List<DotController>();

	public static readonly ReadOnlyCollection<DotController> readOnlyInstancesList = instancesList.AsReadOnly();

	public static GameObject FractureImpactEffectPrefab;

	public static GameObject HelfireIgniteEffectPrefab;

	public static GameObject DotControllerPrefab;

	[SyncVar]
	private NetworkInstanceId victimObjectId;

	private GameObject _victimObject;

	private CharacterBody _victimBody;

	private BurnEffectController burnEffectController;

	private BurnEffectController strongerBurnEffectController;

	private BurnEffectController helfireEffectController;

	private BurnEffectController poisonEffectController;

	private BurnEffectController blightEffectController;

	private EffectManagerHelper bleedEffect;

	private EffectManagerHelper superBleedEffect;

	private EffectManagerHelper preFractureEffect;

	[SyncVar]
	private uint activeDotFlags;

	private static readonly DotStackPool dotStackPool = new DotStackPool();

	private List<DotStack> dotStackList;

	private float[] dotTimers;

	private static readonly PendingDamagePool pendingDamagePool = new PendingDamagePool();

	private int recordedVictimInstanceId = -1;

	public GameObject victimObject
	{
		get
		{
			if (!_victimObject)
			{
				if (NetworkServer.active)
				{
					_victimObject = NetworkServer.FindLocalObject(victimObjectId);
				}
				else if (NetworkClient.active)
				{
					_victimObject = ClientScene.FindLocalObject(victimObjectId);
				}
			}
			return _victimObject;
		}
		set
		{
			NetworkvictimObjectId = value.GetComponent<NetworkIdentity>().netId;
		}
	}

	private CharacterBody victimBody
	{
		get
		{
			if (!_victimBody && (bool)victimObject)
			{
				_victimBody = victimObject.GetComponent<CharacterBody>();
			}
			return _victimBody;
		}
	}

	private HealthComponent victimHealthComponent => victimBody?.healthComponent;

	private TeamIndex victimTeam
	{
		get
		{
			if (!victimBody)
			{
				return TeamIndex.None;
			}
			return victimBody.teamComponent.teamIndex;
		}
	}

	public NetworkInstanceId NetworkvictimObjectId
	{
		get
		{
			return victimObjectId;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref victimObjectId, 1u);
		}
	}

	public uint NetworkactiveDotFlags
	{
		get
		{
			return activeDotFlags;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref activeDotFlags, 2u);
		}
	}

	public static event OnDotInflictedServerGlobalDelegate onDotInflictedServerGlobal;

	public static DotDef GetDotDef(DotIndex dotIndex)
	{
		if (dotIndex < DotIndex.Bleed || dotIndex >= DotIndex.Count)
		{
			return null;
		}
		return dotDefs[(int)dotIndex];
	}

	[SystemInitializer(new Type[] { typeof(BuffCatalog) })]
	private static void InitDotCatalog()
	{
		dotDefs = new DotDef[10];
		dotDefs[0] = new DotDef
		{
			interval = 0.25f,
			damageCoefficient = 0.2f,
			damageColorIndex = DamageColorIndex.Bleed,
			associatedBuff = RoR2Content.Buffs.Bleeding
		};
		dotDefs[1] = new DotDef
		{
			interval = 0.2f,
			damageCoefficient = 0.1f,
			damageColorIndex = DamageColorIndex.Item,
			associatedBuff = RoR2Content.Buffs.OnFire,
			terminalTimedBuff = RoR2Content.Buffs.OnFire,
			terminalTimedBuffDuration = 1f
		};
		dotDefs[7] = new DotDef
		{
			interval = 0.2f,
			damageCoefficient = 0.1f,
			damageColorIndex = DamageColorIndex.Item,
			associatedBuff = DLC1Content.Buffs.StrongerBurn,
			terminalTimedBuff = DLC1Content.Buffs.StrongerBurn,
			terminalTimedBuffDuration = 1f
		};
		dotDefs[3] = new DotDef
		{
			interval = 0.2f,
			damageCoefficient = 0.1f,
			damageColorIndex = DamageColorIndex.Item,
			associatedBuff = RoR2Content.Buffs.OnFire
		};
		dotDefs[2] = new DotDef
		{
			interval = 0.2f,
			damageCoefficient = 0.02f,
			damageColorIndex = DamageColorIndex.Item
		};
		dotDefs[4] = new DotDef
		{
			interval = 0.333f,
			damageCoefficient = 0.333f,
			damageColorIndex = DamageColorIndex.Poison,
			associatedBuff = RoR2Content.Buffs.Poisoned
		};
		dotDefs[5] = new DotDef
		{
			interval = 0.333f,
			damageCoefficient = 0.2f,
			damageColorIndex = DamageColorIndex.Poison,
			associatedBuff = RoR2Content.Buffs.Blight
		};
		dotDefs[6] = new DotDef
		{
			interval = 0.25f,
			damageCoefficient = 0.333f,
			damageColorIndex = DamageColorIndex.SuperBleed,
			associatedBuff = RoR2Content.Buffs.SuperBleed
		};
		dotDefs[8] = new DotDef
		{
			interval = 3f,
			damageCoefficient = 4f,
			damageColorIndex = DamageColorIndex.Void,
			associatedBuff = DLC1Content.Buffs.Fracture,
			resetTimerOnAdd = false
		};
		dotDefs[9] = new DotDef
		{
			interval = 3f,
			damageCoefficient = 0f,
			damageColorIndex = DamageColorIndex.Default,
			associatedBuff = DLC2Content.Buffs.lunarruin,
			resetTimerOnAdd = true
		};
	}

	public bool HasDotActive(DotIndex dotIndex)
	{
		return (activeDotFlags & (1 << (int)dotIndex)) != 0;
	}

	private void Awake()
	{
		if (NetworkServer.active)
		{
			dotStackList = new List<DotStack>();
			dotTimers = new float[10];
		}
		instancesList.Add(this);
	}

	private void OnDestroy()
	{
		if (NetworkServer.active)
		{
			for (int num = dotStackList.Count - 1; num >= 0; num--)
			{
				RemoveDotStackAtServer(num);
			}
		}
		instancesList.Remove(this);
		if (recordedVictimInstanceId != -1)
		{
			dotControllerLocator.Remove(recordedVictimInstanceId);
		}
	}

	private void FixedUpdate()
	{
		if (!victimObject)
		{
			if (NetworkServer.active)
			{
				HandleDestroy();
			}
			return;
		}
		UpdateDotVisuals();
		if (!NetworkServer.active)
		{
			return;
		}
		for (DotIndex dotIndex = DotIndex.Bleed; dotIndex < DotIndex.Count; dotIndex++)
		{
			uint num = (uint)(1 << (int)dotIndex);
			if ((activeDotFlags & num) == 0)
			{
				continue;
			}
			DotDef dotDef = GetDotDef(dotIndex);
			float num2 = dotTimers[(int)dotIndex] - Time.fixedDeltaTime;
			if (num2 <= 0f)
			{
				num2 += dotDef.interval;
				int remainingActive = 0;
				EvaluateDotStacksForType(dotIndex, dotDef.interval, out remainingActive);
				NetworkactiveDotFlags = activeDotFlags & ~num;
				if (remainingActive != 0)
				{
					NetworkactiveDotFlags = activeDotFlags | num;
				}
			}
			dotTimers[(int)dotIndex] = num2;
		}
		if (dotStackList.Count == 0)
		{
			HandleDestroy();
		}
	}

	private void HandleDestroy()
	{
		RepoolEffect(ref bleedEffect);
		RepoolEffect(ref superBleedEffect);
		RepoolEffect(ref preFractureEffect);
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private EffectManagerHelper GetPooledEffect(string _path)
	{
		return EffectManager.GetAndActivatePooledEffect(LegacyResourcesAPI.Load<GameObject>(_path), base.transform, inResetLocal: true);
	}

	private void RepoolEffect(ref EffectManagerHelper emh)
	{
		if ((bool)emh)
		{
			if (emh.OwningPool != null)
			{
				emh.transform.SetParent(null);
				emh.ReturnToPool();
			}
			else
			{
				UnityEngine.Object.Destroy(emh.gameObject);
			}
			emh = null;
		}
	}

	private void UpdateDotVisuals()
	{
		if (!victimBody)
		{
			return;
		}
		ModelLocator modelLocator = null;
		base.transform.position = victimBody.corePosition;
		if ((activeDotFlags & (true ? 1u : 0u)) != 0)
		{
			if (!bleedEffect)
			{
				bleedEffect = GetPooledEffect("Prefabs/BleedEffect");
			}
		}
		else if ((bool)bleedEffect)
		{
			RepoolEffect(ref bleedEffect);
		}
		if ((activeDotFlags & 2u) != 0 || (activeDotFlags & 8u) != 0)
		{
			if (!burnEffectController)
			{
				modelLocator = (modelLocator ? modelLocator : victimObject.GetComponent<ModelLocator>());
				if ((bool)modelLocator && (bool)modelLocator.modelTransform)
				{
					burnEffectController = base.gameObject.AddComponent<BurnEffectController>();
					burnEffectController.effectType = BurnEffectController.normalEffect;
					burnEffectController.target = modelLocator.modelTransform.gameObject;
				}
			}
		}
		else if ((bool)burnEffectController)
		{
			burnEffectController.HandleDestroy();
			burnEffectController = null;
		}
		if ((activeDotFlags & 0x80u) != 0)
		{
			if (!strongerBurnEffectController)
			{
				modelLocator = (modelLocator ? modelLocator : victimObject.GetComponent<ModelLocator>());
				if ((bool)modelLocator && (bool)modelLocator.modelTransform)
				{
					strongerBurnEffectController = base.gameObject.AddComponent<BurnEffectController>();
					strongerBurnEffectController.effectType = BurnEffectController.strongerBurnEffect;
					strongerBurnEffectController.target = modelLocator.modelTransform.gameObject;
				}
			}
		}
		else if ((bool)strongerBurnEffectController)
		{
			strongerBurnEffectController.HandleDestroy();
			strongerBurnEffectController = null;
		}
		if ((activeDotFlags & 4u) != 0)
		{
			if (!helfireEffectController)
			{
				modelLocator = (modelLocator ? modelLocator : victimObject.GetComponent<ModelLocator>());
				if ((bool)modelLocator && (bool)modelLocator.modelTransform)
				{
					helfireEffectController = base.gameObject.AddComponent<BurnEffectController>();
					helfireEffectController.effectType = BurnEffectController.helfireEffect;
					helfireEffectController.target = modelLocator.modelTransform.gameObject;
				}
			}
		}
		else if ((bool)helfireEffectController)
		{
			helfireEffectController.HandleDestroy();
			helfireEffectController = null;
		}
		if ((activeDotFlags & 0x10u) != 0)
		{
			if (!poisonEffectController)
			{
				modelLocator = (modelLocator ? modelLocator : victimObject.GetComponent<ModelLocator>());
				if ((bool)modelLocator && (bool)modelLocator.modelTransform)
				{
					poisonEffectController = base.gameObject.AddComponent<BurnEffectController>();
					poisonEffectController.effectType = BurnEffectController.poisonEffect;
					poisonEffectController.target = modelLocator.modelTransform.gameObject;
				}
			}
		}
		else if ((bool)poisonEffectController)
		{
			poisonEffectController.HandleDestroy();
			poisonEffectController = null;
		}
		if ((activeDotFlags & 0x20u) != 0)
		{
			if (!blightEffectController)
			{
				modelLocator = (modelLocator ? modelLocator : victimObject.GetComponent<ModelLocator>());
				if ((bool)modelLocator && (bool)modelLocator.modelTransform)
				{
					blightEffectController = base.gameObject.AddComponent<BurnEffectController>();
					blightEffectController.effectType = BurnEffectController.blightEffect;
					blightEffectController.target = modelLocator.modelTransform.gameObject;
				}
			}
		}
		else if ((bool)blightEffectController)
		{
			blightEffectController.HandleDestroy();
			blightEffectController = null;
		}
		if ((activeDotFlags & 0x40u) != 0)
		{
			if (!superBleedEffect)
			{
				superBleedEffect = GetPooledEffect("Prefabs/SuperBleedEffect");
			}
		}
		else if ((bool)superBleedEffect)
		{
			RepoolEffect(ref superBleedEffect);
		}
		if ((activeDotFlags & 0x100u) != 0)
		{
			if (!preFractureEffect)
			{
				preFractureEffect = GetPooledEffect("Prefabs/PreFractureEffect");
			}
		}
		else if ((bool)preFractureEffect)
		{
			RepoolEffect(ref preFractureEffect);
		}
	}

	private void LateUpdate()
	{
		if ((bool)victimObject)
		{
			base.transform.position = victimObject.transform.position;
		}
	}

	private static void AddPendingDamageEntry(List<PendingDamage> pendingDamages, GameObject attackerObject, float damage, DamageTypeCombo damageType)
	{
		for (int i = 0; i < pendingDamages.Count; i++)
		{
			if (pendingDamages[i].attackerObject == attackerObject)
			{
				pendingDamages[i].totalDamage += damage;
				return;
			}
		}
		PendingDamage pendingDamage = pendingDamagePool.Request();
		pendingDamage.attackerObject = attackerObject;
		pendingDamage.totalDamage = damage;
		pendingDamage.damageType = damageType;
		pendingDamages.Add(pendingDamage);
	}

	private void OnDotStackAddedServer(DotStack dotStack)
	{
		DotDef dotDef = dotStack.dotDef;
		if (dotDef.associatedBuff != null && (bool)victimBody)
		{
			victimBody.AddBuff(dotDef.associatedBuff.buffIndex);
		}
	}

	private void OnDotStackRemovedServer(DotStack dotStack)
	{
		DotDef dotDef = dotStack.dotDef;
		if ((bool)victimBody)
		{
			if (dotDef.associatedBuff != null)
			{
				victimBody.RemoveBuff(dotDef.associatedBuff.buffIndex);
			}
			if (dotDef.terminalTimedBuff != null)
			{
				victimBody.AddTimedBuff(dotDef.terminalTimedBuff, dotDef.terminalTimedBuffDuration);
			}
		}
	}

	private void RemoveDotStackAtServer(int i)
	{
		DotStack dotStack = dotStackList[i];
		dotStackList.RemoveAt(i);
		OnDotStackRemovedServer(dotStack);
		dotStackPool.Return(dotStack);
	}

	private void EvaluateDotStacksForType(DotIndex dotIndex, float dt, out int remainingActive)
	{
		List<PendingDamage> list = CollectionPool<PendingDamage, List<PendingDamage>>.RentCollection();
		remainingActive = 0;
		DotDef dotDef = GetDotDef(dotIndex);
		for (int num = dotStackList.Count - 1; num >= 0; num--)
		{
			DotStack dotStack = dotStackList[num];
			if (dotStack.dotIndex == dotIndex)
			{
				dotStack.timer -= dt;
				AddPendingDamageEntry(list, dotStack.attackerObject, dotStack.damage, dotStack.damageType);
				if (dotStack.timer <= 0f)
				{
					RemoveDotStackAtServer(num);
				}
				else
				{
					remainingActive++;
				}
			}
		}
		if ((bool)victimObject)
		{
			if ((bool)victimBody && dotIndex == DotIndex.Fracture && list.Count > 0)
			{
				if (FractureImpactEffectPrefab == null)
				{
					FractureImpactEffectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/FractureImpactEffect");
				}
				EffectManager.SpawnEffect(FractureImpactEffectPrefab, new EffectData
				{
					origin = victimBody.corePosition
				}, transmit: true);
			}
			if ((bool)victimHealthComponent)
			{
				Vector3 corePosition = victimBody.corePosition;
				for (int i = 0; i < list.Count; i++)
				{
					DamageInfo damageInfo = new DamageInfo();
					damageInfo.attacker = list[i].attackerObject;
					damageInfo.crit = false;
					damageInfo.damage = list[i].totalDamage;
					damageInfo.force = Vector3.zero;
					damageInfo.inflictor = base.gameObject;
					damageInfo.position = corePosition;
					damageInfo.procCoefficient = 0f;
					damageInfo.damageColorIndex = dotDef.damageColorIndex;
					damageInfo.damageType = list[i].damageType | DamageType.DoT;
					damageInfo.dotIndex = dotIndex;
					victimHealthComponent.TakeDamage(damageInfo);
				}
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			pendingDamagePool.Return(list[j]);
		}
		CollectionPool<PendingDamage, List<PendingDamage>>.ReturnCollection(list);
	}

	[Server]
	private void AddDot(GameObject attackerObject, float duration, DotIndex dotIndex, float damageMultiplier, uint? maxStacksFromAttacker, float? totalDamage, DotIndex? preUpgradeDotIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.DotController::AddDot(UnityEngine.GameObject,System.Single,RoR2.DotController/DotIndex,System.Single,System.Nullable`1<System.UInt32>,System.Nullable`1<System.Single>,System.Nullable`1<RoR2.DotController/DotIndex>)' called on client");
			return;
		}
		TeamIndex attackerTeam = TeamIndex.Neutral;
		float num = 0f;
		TeamComponent component = attackerObject.GetComponent<TeamComponent>();
		if ((bool)component)
		{
			attackerTeam = component.teamIndex;
		}
		CharacterBody component2 = attackerObject.GetComponent<CharacterBody>();
		if ((bool)component2)
		{
			num = component2.damage;
		}
		DotDef dotDef = dotDefs[(int)dotIndex];
		DotStack dotStack = dotStackPool.Request();
		dotStack.dotIndex = dotIndex;
		dotStack.dotDef = dotDef;
		dotStack.attackerObject = attackerObject;
		dotStack.attackerTeam = attackerTeam;
		dotStack.timer = duration;
		dotStack.damage = dotDef.damageCoefficient * num * damageMultiplier;
		dotStack.damageType = DamageType.Generic;
		int num2 = 0;
		int i = 0;
		for (int count = dotStackList.Count; i < count; i++)
		{
			if (dotStackList[i].dotIndex == dotIndex)
			{
				num2++;
			}
		}
		switch (dotIndex)
		{
		case DotIndex.Bleed:
		case DotIndex.SuperBleed:
		case DotIndex.Fracture:
		case DotIndex.LunarRuin:
		{
			int k = 0;
			for (int count3 = dotStackList.Count; k < count3; k++)
			{
				if (dotStackList[k].dotIndex == dotIndex)
				{
					dotStackList[k].timer = Mathf.Max(dotStackList[k].timer, duration);
				}
			}
			break;
		}
		case DotIndex.Burn:
		case DotIndex.StrongerBurn:
			dotStack.damage = Mathf.Min(dotStack.damage, victimBody.healthComponent.fullCombinedHealth * 0.01f * damageMultiplier);
			break;
		case DotIndex.PercentBurn:
			dotStack.damage = Mathf.Min(dotStack.damage, victimBody.healthComponent.fullCombinedHealth * 0.01f);
			break;
		case DotIndex.Helfire:
		{
			if (!component2)
			{
				return;
			}
			HealthComponent healthComponent = component2.healthComponent;
			if (!healthComponent)
			{
				return;
			}
			dotStack.damage = Mathf.Min(healthComponent.fullCombinedHealth * 0.01f * damageMultiplier, victimBody.healthComponent.fullCombinedHealth * 0.01f * damageMultiplier);
			if ((bool)victimBody)
			{
				if (HelfireIgniteEffectPrefab == null)
				{
					HelfireIgniteEffectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/HelfireIgniteEffect");
				}
				EffectManager.SpawnEffect(HelfireIgniteEffectPrefab, new EffectData
				{
					origin = victimBody.corePosition
				}, transmit: true);
			}
			break;
		}
		case DotIndex.Poison:
		{
			float a = victimHealthComponent.fullCombinedHealth / 100f * 1f * dotDef.interval;
			dotStack.damage = Mathf.Min(Mathf.Max(a, dotStack.damage), dotStack.damage * 50f);
			dotStack.damageType = DamageType.NonLethal;
			int j = 0;
			for (int count2 = dotStackList.Count; j < count2; j++)
			{
				if (dotStackList[j].dotIndex == DotIndex.Poison)
				{
					dotStackList[j].timer = Mathf.Max(dotStackList[j].timer, duration);
					dotStackList[j].damage = dotStack.damage;
					return;
				}
			}
			if (num2 == 0)
			{
				component2?.master?.playerStatsComponent?.currentStats.PushStatValue(StatDef.totalCrocoInfectionsInflicted, 1uL);
			}
			break;
		}
		}
		if ((dotIndex == DotIndex.Helfire || (preUpgradeDotIndex.HasValue && preUpgradeDotIndex.Value == DotIndex.Helfire)) && victimObject == attackerObject)
		{
			dotStack.damageType |= (DamageTypeCombo)(DamageType.NonLethal | DamageType.Silent);
		}
		dotStack.damage = Mathf.Max(1f, dotStack.damage);
		if (totalDamage.HasValue && dotStack.damage != 0f)
		{
			duration = totalDamage.Value * dotDef.interval / dotStack.damage;
			dotStack.timer = duration;
		}
		if (maxStacksFromAttacker.HasValue)
		{
			DotStack dotStack2 = null;
			int num3 = 0;
			int l = 0;
			for (int count4 = dotStackList.Count; l < count4; l++)
			{
				DotStack dotStack3 = dotStackList[l];
				if (dotStack3.dotIndex == dotIndex && dotStack3.attackerObject == attackerObject)
				{
					num3++;
					if (dotStack2 == null || dotStack3.timer < dotStack2.timer)
					{
						dotStack2 = dotStack3;
					}
				}
			}
			if (num3 >= maxStacksFromAttacker.Value && dotStack2 != null)
			{
				if (dotStack2.timer < duration)
				{
					dotStack2.timer = duration;
					dotStack2.damage = dotStack.damage;
					dotStack2.damageType = dotStack.damageType;
				}
				return;
			}
		}
		if (num2 == 0 || dotDef.resetTimerOnAdd)
		{
			NetworkactiveDotFlags = activeDotFlags | (uint)(1 << (int)dotIndex);
			dotTimers[(int)dotIndex] = dotDef.interval;
		}
		dotStackList.Add(dotStack);
		OnDotStackAddedServer(dotStack);
	}

	[Server]
	public static void InflictDot(ref InflictDotInfo inflictDotInfo)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.DotController::InflictDot(RoR2.InflictDotInfo&)' called on client");
		}
		else
		{
			if (inflictDotInfo.dotIndex < DotIndex.Bleed || inflictDotInfo.dotIndex >= DotIndex.Count || ImmuneToDebuffBehavior.OverrideDot(inflictDotInfo) || !inflictDotInfo.victimObject || !inflictDotInfo.attackerObject)
			{
				return;
			}
			if (!dotControllerLocator.TryGetValue(inflictDotInfo.victimObject.GetInstanceID(), out var value))
			{
				if (DotControllerPrefab == null)
				{
					DotControllerPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/DotController");
				}
				GameObject obj = UnityEngine.Object.Instantiate(DotControllerPrefab);
				value = obj.GetComponent<DotController>();
				value.victimObject = inflictDotInfo.victimObject;
				value.recordedVictimInstanceId = inflictDotInfo.victimObject.GetInstanceID();
				dotControllerLocator.Add(value.recordedVictimInstanceId, value);
				NetworkServer.Spawn(obj);
			}
			value.AddDot(inflictDotInfo.attackerObject, inflictDotInfo.duration, inflictDotInfo.dotIndex, inflictDotInfo.damageMultiplier, inflictDotInfo.maxStacksFromAttacker, inflictDotInfo.totalDamage, inflictDotInfo.preUpgradeDotIndex);
			DotController.onDotInflictedServerGlobal?.Invoke(value, ref inflictDotInfo);
		}
	}

	[Server]
	public static void InflictDot(GameObject victimObject, GameObject attackerObject, DotIndex dotIndex, float duration = 8f, float damageMultiplier = 1f, uint? maxStacksFromAttacker = null)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.DotController::InflictDot(UnityEngine.GameObject,UnityEngine.GameObject,RoR2.DotController/DotIndex,System.Single,System.Single,System.Nullable`1<System.UInt32>)' called on client");
			return;
		}
		InflictDotInfo inflictDotInfo = default(InflictDotInfo);
		inflictDotInfo.victimObject = victimObject;
		inflictDotInfo.attackerObject = attackerObject;
		inflictDotInfo.dotIndex = dotIndex;
		inflictDotInfo.duration = duration;
		inflictDotInfo.damageMultiplier = damageMultiplier;
		inflictDotInfo.maxStacksFromAttacker = maxStacksFromAttacker;
		InflictDotInfo inflictDotInfo2 = inflictDotInfo;
		InflictDot(ref inflictDotInfo2);
	}

	[Server]
	public static void RemoveAllDots(GameObject victimObject)
	{
		DotController value;
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.DotController::RemoveAllDots(UnityEngine.GameObject)' called on client");
		}
		else if (dotControllerLocator.TryGetValue(victimObject.GetInstanceID(), out value))
		{
			value.HandleDestroy();
		}
	}

	[Server]
	public static DotController FindDotController(GameObject victimObject)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'RoR2.DotController RoR2.DotController::FindDotController(UnityEngine.GameObject)' called on client");
			return null;
		}
		int i = 0;
		for (int count = instancesList.Count; i < count; i++)
		{
			if ((object)victimObject == instancesList[i]._victimObject)
			{
				return instancesList[i];
			}
		}
		return null;
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(victimObjectId);
			writer.WritePackedUInt32(activeDotFlags);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(victimObjectId);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32(activeDotFlags);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			victimObjectId = reader.ReadNetworkId();
			activeDotFlags = reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			victimObjectId = reader.ReadNetworkId();
		}
		if (((uint)num & 2u) != 0)
		{
			activeDotFlags = reader.ReadPackedUInt32();
		}
	}

	public override void PreStartClient()
	{
	}
}

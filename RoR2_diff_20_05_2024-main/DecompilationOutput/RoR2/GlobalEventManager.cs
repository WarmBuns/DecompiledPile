using System;
using System.Collections.Generic;
using System.Linq;
using HG;
using JetBrains.Annotations;
using RoR2.Orbs;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2;

public class GlobalEventManager : MonoBehaviour
{
	private static class CommonAssets
	{
		public static CharacterMaster wispSoulMasterPrefabMasterComponent;

		public static GameObject igniteOnKillExplosionEffectPrefab;

		public static GameObject missilePrefab;

		public static GameObject explodeOnDeathPrefab;

		public static GameObject daggerPrefab;

		public static GameObject bleedOnHitAndExplodeImpactEffect;

		public static GameObject bleedOnHitAndExplodeBlastEffect;

		public static GameObject minorConstructOnKillProjectile;

		public static GameObject knockbackFinEffect;

		public static GameObject runicMeteorEffect;

		public static GameObject missileVoidPrefab;

		public static GameObject eliteEarthHealerMaster;

		public static PickupDropTable dtSonorousEchoPath;

		public static GameObject loseCoinsImpactEffectPrefab;

		public static void Load()
		{
			AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/CharacterMasters/WispSoulMaster");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				wispSoulMasterPrefabMasterComponent = x.Result.GetComponent<CharacterMaster>();
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/ImpactEffects/IgniteExplosionVFX");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				igniteOnKillExplosionEffectPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Projectiles/MissileProjectile");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				missilePrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Projectiles/DaggerProjectile");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				daggerPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/ImpactEffects/BleedOnHitAndExplode_Impact");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				bleedOnHitAndExplodeImpactEffect = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/NetworkedObjects/BleedOnHitAndExplodeDelay");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				bleedOnHitAndExplodeBlastEffect = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Projectiles/MissileVoidProjectile");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				missileVoidPrefab = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/ImpactEffects/KnockBackHitEnemiesImpact");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				knockbackFinEffect = x.Result;
			};
			asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/Effects/RunicMeteorStrikePredictionEffect");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				runicMeteorEffect = x.Result;
			};
			asyncOperationHandle = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/EliteEarth/AffixEarthHealerMaster.prefab");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				eliteEarthHealerMaster = x.Result;
			};
			asyncOperationHandle = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ExplodeOnDeath/WilloWispDelay.prefab");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				explodeOnDeathPrefab = x.Result;
			};
			asyncOperationHandle = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/MinorConstructOnKill/MinorConstructOnKillProjectile.prefab");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				minorConstructOnKillProjectile = x.Result;
			};
			AsyncOperationHandle<PickupDropTable> asyncOperationHandle2 = Addressables.LoadAssetAsync<PickupDropTable>("RoR2/DLC2/Items/ResetChests/dtSonorousEcho.asset");
			asyncOperationHandle2.Completed += delegate(AsyncOperationHandle<PickupDropTable> x)
			{
				dtSonorousEchoPath = x.Result;
			};
			asyncOperationHandle = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/CoinImpact.prefab");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
			{
				loseCoinsImpactEffectPrefab = x.Result;
			};
		}
	}

	public static GlobalEventManager instance;

	[Obsolete("Transform of the global event manager should not be used! You probably meant something else instead.", true)]
	private new Transform transform;

	[Obsolete("GameObject of the global event manager should not be used! You probably meant something else instead.", true)]
	private new GameObject gameObject;

	private static readonly string[] standardDeathQuoteTokens = (from i in Enumerable.Range(0, 37)
		select "PLAYER_DEATH_QUOTE_" + TextSerialization.ToStringInvariant(i)).ToArray();

	private static readonly string[] fallDamageDeathQuoteTokens = (from i in Enumerable.Range(0, 5)
		select "PLAYER_DEATH_QUOTE_FALLDAMAGE_" + TextSerialization.ToStringInvariant(i)).ToArray();

	private static readonly SphereSearch igniteOnKillSphereSearch = new SphereSearch();

	private static readonly List<HurtBox> igniteOnKillHurtBoxBuffer = new List<HurtBox>();

	public static event Action<DamageReport> onCharacterDeathGlobal;

	public static event Action<TeamIndex> onTeamLevelUp;

	public static event Action<CharacterBody> onCharacterLevelUp;

	public static event Action<Interactor, IInteractable, GameObject> OnInteractionsGlobal;

	public static event Action<DamageDealtMessage> onClientDamageNotified;

	public static event Action<DamageReport> onServerDamageDealt;

	public static event Action<DamageReport, float> onServerCharacterExecuted;

	[InitDuringStartup]
	private static void Init()
	{
		CommonAssets.Load();
	}

	private void OnEnable()
	{
		if ((bool)instance)
		{
			Debug.LogError("Only one GlobalEventManager can exist at a time.");
		}
		else
		{
			instance = this;
		}
	}

	private void OnDisable()
	{
		if (instance == this)
		{
			instance = null;
		}
	}

	public void OnHitEnemy([NotNull] DamageInfo damageInfo, [NotNull] GameObject victim)
	{
		ProcessHitEnemy(damageInfo, victim);
	}

	private void ProcessHitEnemy([NotNull] DamageInfo damageInfo, [NotNull] GameObject victim)
	{
		if (damageInfo.procCoefficient == 0f || damageInfo.rejected || !NetworkServer.active)
		{
			return;
		}
		if (damageInfo.attacker == null)
		{
			HandleDamageWithNoAttacker(damageInfo, victim);
		}
		if (!damageInfo.attacker || !(damageInfo.procCoefficient > 0f))
		{
			return;
		}
		uint? maxStacksFromAttacker = null;
		if ((bool)damageInfo?.inflictor)
		{
			ProjectileDamage component = damageInfo.inflictor.GetComponent<ProjectileDamage>();
			if ((bool)component && component.useDotMaxStacksFromAttacker)
			{
				maxStacksFromAttacker = component.dotMaxStacksFromAttacker;
			}
		}
		CharacterBody component2 = damageInfo.attacker.GetComponent<CharacterBody>();
		CharacterBody characterBody = (victim ? victim.GetComponent<CharacterBody>() : null);
		if ((ulong)(damageInfo.damageType & DamageType.LunarRuin) != 0)
		{
			if ((ulong)(damageInfo.damageType & DamageTypeExtended.ApplyBuffPermanently) != 0L)
			{
				characterBody.AddTimedBuff(DLC2Content.Buffs.lunarruin, 420f);
			}
			else
			{
				DotController.InflictDot(characterBody.gameObject, damageInfo.attacker, DotController.DotIndex.LunarRuin, 5f);
			}
		}
		if ((ulong)(damageInfo.damageType & DamageTypeExtended.DisableAllSkills) != 0L)
		{
			if ((ulong)(damageInfo.damageType & DamageTypeExtended.ApplyBuffPermanently) != 0L)
			{
				Debug.LogFormat("Adding DisableAllSkills");
				characterBody.AddTimedBuff(DLC2Content.Buffs.DisableAllSkills, 420f);
			}
			else
			{
				Debug.LogFormat("Adding DisableAllSkills for 5s");
				characterBody.AddTimedBuff(DLC2Content.Buffs.DisableAllSkills, 5f);
			}
		}
		if (!component2)
		{
			return;
		}
		if ((ulong)(damageInfo.damageType & DamageType.PoisonOnHit) != 0)
		{
			DotController.InflictDot(victim, damageInfo.attacker, DotController.DotIndex.Poison, 10f * damageInfo.procCoefficient, 1f, maxStacksFromAttacker);
		}
		CharacterMaster master = component2.master;
		if (!master)
		{
			return;
		}
		Inventory inventory = master.inventory;
		TeamComponent component3 = component2.GetComponent<TeamComponent>();
		TeamIndex teamIndex = (component3 ? component3.teamIndex : TeamIndex.Neutral);
		Vector3 aimOrigin = component2.aimOrigin;
		if (damageInfo.crit)
		{
			instance.OnCrit(component2, damageInfo, master, damageInfo.procCoefficient, damageInfo.procChainMask);
		}
		if (!damageInfo.procChainMask.HasProc(ProcType.HealOnHit))
		{
			int itemCount = inventory.GetItemCount(RoR2Content.Items.Seed);
			if (itemCount > 0)
			{
				HealthComponent component4 = component2.GetComponent<HealthComponent>();
				if ((bool)component4)
				{
					ProcChainMask procChainMask = damageInfo.procChainMask;
					procChainMask.AddProc(ProcType.HealOnHit);
					component4.Heal((float)itemCount * damageInfo.procCoefficient, procChainMask);
				}
			}
		}
		if (!damageInfo.procChainMask.HasProc(ProcType.BleedOnHit))
		{
			bool flag = (ulong)(damageInfo.damageType & DamageType.BleedOnHit) != 0 || (inventory.GetItemCount(RoR2Content.Items.BleedOnHitAndExplode) > 0 && damageInfo.crit);
			if ((component2.bleedChance > 0f || flag) && (flag || Util.CheckRoll(damageInfo.procCoefficient * component2.bleedChance, master)))
			{
				ProcChainMask procChainMask2 = damageInfo.procChainMask;
				procChainMask2.AddProc(ProcType.BleedOnHit);
				DotController.InflictDot(victim, damageInfo.attacker, DotController.DotIndex.Bleed, 3f * damageInfo.procCoefficient, 1f, maxStacksFromAttacker);
			}
		}
		if (!damageInfo.procChainMask.HasProc(ProcType.FractureOnHit))
		{
			int itemCount2 = inventory.GetItemCount(DLC1Content.Items.BleedOnHitVoid);
			itemCount2 += (component2.HasBuff(DLC1Content.Buffs.EliteVoid) ? 10 : 0);
			if (itemCount2 > 0 && Util.CheckRoll(damageInfo.procCoefficient * (float)itemCount2 * 10f, master))
			{
				ProcChainMask procChainMask3 = damageInfo.procChainMask;
				procChainMask3.AddProc(ProcType.FractureOnHit);
				DotController.DotDef dotDef = DotController.GetDotDef(DotController.DotIndex.Fracture);
				DotController.InflictDot(victim, damageInfo.attacker, DotController.DotIndex.Fracture, dotDef.interval, 1f, maxStacksFromAttacker);
			}
		}
		bool flag2 = (ulong)(damageInfo.damageType & DamageType.BlightOnHit) != 0;
		if (flag2 && flag2)
		{
			_ = damageInfo.procChainMask;
			DotController.InflictDot(victim, damageInfo.attacker, DotController.DotIndex.Blight, 5f * damageInfo.procCoefficient, 1f, maxStacksFromAttacker);
		}
		if ((ulong)(damageInfo.damageType & DamageType.WeakOnHit) != 0)
		{
			characterBody.AddTimedBuff(RoR2Content.Buffs.Weak, 6f * damageInfo.procCoefficient);
		}
		if ((ulong)(damageInfo.damageType & DamageType.IgniteOnHit) != 0 || component2.HasBuff(RoR2Content.Buffs.AffixRed))
		{
			float num = 0.5f;
			InflictDotInfo inflictDotInfo = default(InflictDotInfo);
			inflictDotInfo.attackerObject = damageInfo.attacker;
			inflictDotInfo.victimObject = victim;
			inflictDotInfo.totalDamage = damageInfo.damage * num;
			inflictDotInfo.damageMultiplier = 1f;
			inflictDotInfo.dotIndex = DotController.DotIndex.Burn;
			inflictDotInfo.maxStacksFromAttacker = maxStacksFromAttacker;
			InflictDotInfo dotInfo = inflictDotInfo;
			StrengthenBurnUtils.CheckDotForUpgrade(inventory, ref dotInfo);
			DotController.InflictDot(ref dotInfo);
		}
		float num2 = (component2.HasBuff(RoR2Content.Buffs.AffixWhite) ? 1 : 0);
		num2 += (component2.HasBuff(RoR2Content.Buffs.AffixHaunted) ? 2 : 0);
		if (num2 > 0 && (bool)characterBody)
		{
			EffectManager.SimpleImpactEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/AffixWhiteImpactEffect"), damageInfo.position, Vector3.up, transmit: true);
			characterBody.AddTimedBuff(RoR2Content.Buffs.Slow80, 1.5f * damageInfo.procCoefficient * (float)num2);
		}
		int itemCount3 = master.inventory.GetItemCount(RoR2Content.Items.SlowOnHit);
		if (itemCount3 > 0 && (bool)characterBody)
		{
			characterBody.AddTimedBuff(RoR2Content.Buffs.Slow60, 2f * (float)itemCount3);
		}
		int itemCount4 = master.inventory.GetItemCount(DLC1Content.Items.SlowOnHitVoid);
		if (itemCount4 > 0 && (bool)characterBody && Util.CheckRoll(Util.ConvertAmplificationPercentageIntoReductionPercentage(5f * (float)itemCount4 * damageInfo.procCoefficient), master))
		{
			characterBody.AddTimedBuff(RoR2Content.Buffs.Nullified, 1f * (float)itemCount4);
		}
		if ((component2.HasBuff(RoR2Content.Buffs.AffixPoison) ? 1 : 0) > 0 && (bool)characterBody)
		{
			characterBody.AddTimedBuff(RoR2Content.Buffs.HealingDisabled, 8f * damageInfo.procCoefficient);
		}
		int itemCount5 = inventory.GetItemCount(RoR2Content.Items.GoldOnHit);
		if (itemCount5 > 0 && Util.CheckRoll(30f * damageInfo.procCoefficient, master))
		{
			GoldOrb goldOrb = new GoldOrb();
			goldOrb.origin = damageInfo.position;
			goldOrb.target = component2.mainHurtBox;
			goldOrb.goldAmount = (uint)((float)itemCount5 * 2f * Run.instance.difficultyCoefficient);
			OrbManager.instance.AddOrb(goldOrb);
			EffectManager.SimpleImpactEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/CoinImpact"), damageInfo.position, Vector3.up, transmit: true);
		}
		if (!damageInfo.procChainMask.HasProc(ProcType.Missile))
		{
			int itemCount6 = inventory.GetItemCount(RoR2Content.Items.Missile);
			if (itemCount6 > 0)
			{
				if (Util.CheckRoll(10f * damageInfo.procCoefficient, master))
				{
					float damageCoefficient = 3f * (float)itemCount6;
					float missileDamage = Util.OnHitProcDamage(damageInfo.damage, component2.damage, damageCoefficient);
					MissileUtils.FireMissile(component2.corePosition, component2, damageInfo.procChainMask, victim, missileDamage, damageInfo.crit, CommonAssets.missilePrefab, DamageColorIndex.Item, addMissileProc: true);
				}
			}
			else
			{
				itemCount6 = inventory.GetItemCount(DLC1Content.Items.MissileVoid);
				if (itemCount6 > 0 && component2.healthComponent.shield > 0f)
				{
					int valueOrDefault = (component2?.inventory?.GetItemCount(DLC1Content.Items.MoreMissile)).GetValueOrDefault();
					float num3 = Mathf.Max(1f, 1f + 0.5f * (float)(valueOrDefault - 1));
					float damageCoefficient2 = 0.4f * (float)itemCount6;
					float damageValue = Util.OnHitProcDamage(damageInfo.damage, component2.damage, damageCoefficient2) * num3;
					int num4 = ((valueOrDefault <= 0) ? 1 : 3);
					for (int i = 0; i < num4; i++)
					{
						MissileVoidOrb missileVoidOrb = new MissileVoidOrb();
						missileVoidOrb.origin = aimOrigin;
						missileVoidOrb.damageValue = damageValue;
						missileVoidOrb.isCrit = damageInfo.crit;
						missileVoidOrb.teamIndex = teamIndex;
						missileVoidOrb.attacker = damageInfo.attacker;
						missileVoidOrb.procChainMask = damageInfo.procChainMask;
						missileVoidOrb.procChainMask.AddProc(ProcType.Missile);
						missileVoidOrb.procCoefficient = 0.2f;
						missileVoidOrb.damageColorIndex = DamageColorIndex.Void;
						HurtBox mainHurtBox = characterBody.mainHurtBox;
						if ((bool)mainHurtBox)
						{
							missileVoidOrb.target = mainHurtBox;
							OrbManager.instance.AddOrb(missileVoidOrb);
						}
					}
				}
			}
		}
		if (component2.HasBuff(JunkContent.Buffs.LoaderPylonPowered) && !damageInfo.procChainMask.HasProc(ProcType.LoaderLightning))
		{
			float damageCoefficient3 = 0.3f;
			float damageValue2 = Util.OnHitProcDamage(damageInfo.damage, component2.damage, damageCoefficient3);
			LightningOrb lightningOrb = new LightningOrb();
			lightningOrb.origin = damageInfo.position;
			lightningOrb.damageValue = damageValue2;
			lightningOrb.isCrit = damageInfo.crit;
			lightningOrb.bouncesRemaining = 3;
			lightningOrb.teamIndex = teamIndex;
			lightningOrb.attacker = damageInfo.attacker;
			lightningOrb.bouncedObjects = new List<HealthComponent> { victim.GetComponent<HealthComponent>() };
			lightningOrb.procChainMask = damageInfo.procChainMask;
			lightningOrb.procChainMask.AddProc(ProcType.LoaderLightning);
			lightningOrb.procCoefficient = 0f;
			lightningOrb.lightningType = LightningOrb.LightningType.Loader;
			lightningOrb.damageColorIndex = DamageColorIndex.Item;
			lightningOrb.range = 20f;
			HurtBox hurtBox = lightningOrb.PickNextTarget(damageInfo.position);
			if ((bool)hurtBox)
			{
				lightningOrb.target = hurtBox;
				OrbManager.instance.AddOrb(lightningOrb);
			}
		}
		int itemCount7 = inventory.GetItemCount(RoR2Content.Items.ChainLightning);
		float num5 = 25f;
		if (itemCount7 > 0 && !damageInfo.procChainMask.HasProc(ProcType.ChainLightning) && Util.CheckRoll(num5 * damageInfo.procCoefficient, master))
		{
			float damageCoefficient4 = 0.8f;
			float damageValue3 = Util.OnHitProcDamage(damageInfo.damage, component2.damage, damageCoefficient4);
			LightningOrb lightningOrb2 = new LightningOrb();
			lightningOrb2.origin = damageInfo.position;
			lightningOrb2.damageValue = damageValue3;
			lightningOrb2.isCrit = damageInfo.crit;
			lightningOrb2.bouncesRemaining = 2 * itemCount7;
			lightningOrb2.teamIndex = teamIndex;
			lightningOrb2.attacker = damageInfo.attacker;
			lightningOrb2.bouncedObjects = new List<HealthComponent> { victim.GetComponent<HealthComponent>() };
			lightningOrb2.procChainMask = damageInfo.procChainMask;
			lightningOrb2.procChainMask.AddProc(ProcType.ChainLightning);
			lightningOrb2.procCoefficient = 0.2f;
			lightningOrb2.lightningType = LightningOrb.LightningType.Ukulele;
			lightningOrb2.damageColorIndex = DamageColorIndex.Item;
			lightningOrb2.range += 2 * itemCount7;
			HurtBox hurtBox2 = lightningOrb2.PickNextTarget(damageInfo.position);
			if ((bool)hurtBox2)
			{
				lightningOrb2.target = hurtBox2;
				OrbManager.instance.AddOrb(lightningOrb2);
			}
		}
		int itemCount8 = inventory.GetItemCount(DLC1Content.Items.ChainLightningVoid);
		float num6 = 25f;
		if (itemCount8 > 0 && !damageInfo.procChainMask.HasProc(ProcType.ChainLightning) && Util.CheckRoll(num6 * damageInfo.procCoefficient, master))
		{
			float damageCoefficient5 = 0.6f;
			float damageValue4 = Util.OnHitProcDamage(damageInfo.damage, component2.damage, damageCoefficient5);
			VoidLightningOrb voidLightningOrb = new VoidLightningOrb();
			voidLightningOrb.origin = damageInfo.position;
			voidLightningOrb.damageValue = damageValue4;
			voidLightningOrb.isCrit = damageInfo.crit;
			voidLightningOrb.totalStrikes = 3 * itemCount8;
			voidLightningOrb.teamIndex = teamIndex;
			voidLightningOrb.attacker = damageInfo.attacker;
			voidLightningOrb.procChainMask = damageInfo.procChainMask;
			voidLightningOrb.procChainMask.AddProc(ProcType.ChainLightning);
			voidLightningOrb.procCoefficient = 0.2f;
			voidLightningOrb.damageColorIndex = DamageColorIndex.Void;
			voidLightningOrb.secondsPerStrike = 0.1f;
			HurtBox mainHurtBox2 = characterBody.mainHurtBox;
			if ((bool)mainHurtBox2)
			{
				voidLightningOrb.target = mainHurtBox2;
				OrbManager.instance.AddOrb(voidLightningOrb);
			}
		}
		int itemCount9 = inventory.GetItemCount(RoR2Content.Items.BounceNearby);
		if (itemCount9 > 0)
		{
			float num7 = (1f - 100f / (100f + 20f * (float)itemCount9)) * 100f;
			if (!damageInfo.procChainMask.HasProc(ProcType.BounceNearby) && Util.CheckRoll(num7 * damageInfo.procCoefficient, master))
			{
				List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
				int maxTargets = 5 + itemCount9 * 5;
				BullseyeSearch search = new BullseyeSearch();
				List<HealthComponent> list2 = CollectionPool<HealthComponent, List<HealthComponent>>.RentCollection();
				if ((bool)component2 && (bool)component2.healthComponent)
				{
					list2.Add(component2.healthComponent);
				}
				if ((bool)characterBody && (bool)characterBody.healthComponent)
				{
					list2.Add(characterBody.healthComponent);
				}
				BounceOrb.SearchForTargets(search, teamIndex, damageInfo.position, 30f, maxTargets, list, list2);
				CollectionPool<HealthComponent, List<HealthComponent>>.ReturnCollection(list2);
				List<HealthComponent> bouncedObjects = new List<HealthComponent> { victim.GetComponent<HealthComponent>() };
				float damageCoefficient6 = 1f;
				float damageValue5 = Util.OnHitProcDamage(damageInfo.damage, component2.damage, damageCoefficient6);
				int j = 0;
				for (int count = list.Count; j < count; j++)
				{
					HurtBox hurtBox3 = list[j];
					if ((bool)hurtBox3)
					{
						BounceOrb bounceOrb = new BounceOrb();
						bounceOrb.origin = damageInfo.position;
						bounceOrb.damageValue = damageValue5;
						bounceOrb.isCrit = damageInfo.crit;
						bounceOrb.teamIndex = teamIndex;
						bounceOrb.attacker = damageInfo.attacker;
						bounceOrb.procChainMask = damageInfo.procChainMask;
						bounceOrb.procChainMask.AddProc(ProcType.BounceNearby);
						bounceOrb.procCoefficient = 0.33f;
						bounceOrb.damageColorIndex = DamageColorIndex.Item;
						bounceOrb.bouncedObjects = bouncedObjects;
						bounceOrb.target = hurtBox3;
						OrbManager.instance.AddOrb(bounceOrb);
					}
				}
				CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
			}
		}
		int itemCount10 = inventory.GetItemCount(RoR2Content.Items.StickyBomb);
		if (itemCount10 > 0 && Util.CheckRoll(5f * (float)itemCount10 * damageInfo.procCoefficient, master) && (bool)characterBody)
		{
			bool alive = characterBody.healthComponent.alive;
			float num8 = 5f;
			Vector3 position = damageInfo.position;
			Vector3 forward = characterBody.corePosition - position;
			float magnitude = forward.magnitude;
			Quaternion rotation = ((magnitude != 0f) ? Util.QuaternionSafeLookRotation(forward) : UnityEngine.Random.rotationUniform);
			float damageCoefficient7 = 1.8f;
			float damage = Util.OnHitProcDamage(damageInfo.damage, component2.damage, damageCoefficient7);
			ProjectileManager.instance.FireProjectile(LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/StickyBomb"), position, rotation, damageInfo.attacker, damage, 100f, damageInfo.crit, DamageColorIndex.Item, null, alive ? (magnitude * num8) : (-1f));
		}
		if (!damageInfo.procChainMask.HasProc(ProcType.Rings) && damageInfo.damage / component2.damage >= 4f)
		{
			if (component2.HasBuff(RoR2Content.Buffs.ElementalRingsReady))
			{
				int itemCount11 = inventory.GetItemCount(RoR2Content.Items.IceRing);
				int itemCount12 = inventory.GetItemCount(RoR2Content.Items.FireRing);
				component2.RemoveBuff(RoR2Content.Buffs.ElementalRingsReady);
				for (int k = 1; (float)k <= 10f; k++)
				{
					component2.AddTimedBuff(RoR2Content.Buffs.ElementalRingsCooldown, k);
				}
				ProcChainMask procChainMask4 = damageInfo.procChainMask;
				procChainMask4.AddProc(ProcType.Rings);
				Vector3 position2 = damageInfo.position;
				if (itemCount11 > 0)
				{
					float damageCoefficient8 = 2.5f * (float)itemCount11;
					float damage2 = Util.OnHitProcDamage(damageInfo.damage, component2.damage, damageCoefficient8);
					DamageInfo damageInfo2 = new DamageInfo
					{
						damage = damage2,
						damageColorIndex = DamageColorIndex.Item,
						damageType = DamageType.Generic,
						attacker = damageInfo.attacker,
						crit = damageInfo.crit,
						force = Vector3.zero,
						inflictor = null,
						position = position2,
						procChainMask = procChainMask4,
						procCoefficient = 1f
					};
					EffectManager.SimpleImpactEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/IceRingExplosion"), position2, Vector3.up, transmit: true);
					characterBody.AddTimedBuff(RoR2Content.Buffs.Slow80, 3f * (float)itemCount11);
					characterBody.healthComponent.TakeDamage(damageInfo2);
				}
				if (itemCount12 > 0)
				{
					GameObject gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/FireTornado");
					float resetInterval = gameObject.GetComponent<ProjectileOverlapAttack>().resetInterval;
					float lifetime = gameObject.GetComponent<ProjectileSimple>().lifetime;
					float damageCoefficient9 = 3f * (float)itemCount12;
					float damage3 = Util.OnHitProcDamage(damageInfo.damage, component2.damage, damageCoefficient9) / lifetime * resetInterval;
					float speedOverride = 0f;
					Quaternion rotation2 = Quaternion.identity;
					Vector3 vector = position2 - aimOrigin;
					vector.y = 0f;
					if (vector != Vector3.zero)
					{
						speedOverride = -1f;
						rotation2 = Util.QuaternionSafeLookRotation(vector, Vector3.up);
					}
					ProjectileManager.instance.FireProjectile(new FireProjectileInfo
					{
						damage = damage3,
						crit = damageInfo.crit,
						damageColorIndex = DamageColorIndex.Item,
						position = position2,
						procChainMask = procChainMask4,
						force = 0f,
						owner = damageInfo.attacker,
						projectilePrefab = gameObject,
						rotation = rotation2,
						speedOverride = speedOverride,
						target = null
					});
				}
			}
			else if (component2.HasBuff(DLC1Content.Buffs.ElementalRingVoidReady))
			{
				int itemCount13 = inventory.GetItemCount(DLC1Content.Items.ElementalRingVoid);
				component2.RemoveBuff(DLC1Content.Buffs.ElementalRingVoidReady);
				for (int l = 1; (float)l <= 20f; l++)
				{
					component2.AddTimedBuff(DLC1Content.Buffs.ElementalRingVoidCooldown, l);
				}
				ProcChainMask procChainMask5 = damageInfo.procChainMask;
				procChainMask5.AddProc(ProcType.Rings);
				if (itemCount13 > 0)
				{
					GameObject projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/ElementalRingVoidBlackHole");
					float damageCoefficient10 = 1f * (float)itemCount13;
					float damage4 = Util.OnHitProcDamage(damageInfo.damage, component2.damage, damageCoefficient10);
					ProjectileManager.instance.FireProjectile(new FireProjectileInfo
					{
						damage = damage4,
						crit = damageInfo.crit,
						damageColorIndex = DamageColorIndex.Void,
						position = damageInfo.position,
						procChainMask = procChainMask5,
						force = 6000f,
						owner = damageInfo.attacker,
						projectilePrefab = projectilePrefab,
						rotation = Quaternion.identity,
						target = null
					});
				}
			}
		}
		int itemCount14 = master.inventory.GetItemCount(RoR2Content.Items.DeathMark);
		int num9 = 0;
		if (itemCount14 >= 1 && !characterBody.HasBuff(RoR2Content.Buffs.DeathMark))
		{
			BuffIndex[] debuffBuffIndices = BuffCatalog.debuffBuffIndices;
			foreach (BuffIndex buffType in debuffBuffIndices)
			{
				if (characterBody.HasBuff(buffType))
				{
					num9++;
				}
			}
			DotController dotController = DotController.FindDotController(victim.gameObject);
			if ((bool)dotController)
			{
				for (DotController.DotIndex dotIndex = DotController.DotIndex.Bleed; dotIndex < DotController.DotIndex.Count; dotIndex++)
				{
					if (dotController.HasDotActive(dotIndex))
					{
						num9++;
					}
				}
			}
			if (num9 >= 4)
			{
				characterBody.AddTimedBuff(RoR2Content.Buffs.DeathMark, 7f * (float)itemCount14);
			}
		}
		if (damageInfo != null && damageInfo.inflictor != null && damageInfo.inflictor.GetComponent<BoomerangProjectile>() != null && !damageInfo.procChainMask.HasProc(ProcType.BleedOnHit))
		{
			int num10 = 0;
			if (inventory.GetEquipmentIndex() == RoR2Content.Equipment.Saw.equipmentIndex)
			{
				num10 = 1;
			}
			bool flag3 = (ulong)(damageInfo.damageType & DamageType.BleedOnHit) != 0;
			if ((num10 > 0 || flag3) && (flag3 || Util.CheckRoll(100f, master)))
			{
				ProcChainMask procChainMask6 = damageInfo.procChainMask;
				procChainMask6.AddProc(ProcType.BleedOnHit);
				DotController.InflictDot(victim, damageInfo.attacker, DotController.DotIndex.Bleed, 4f * damageInfo.procCoefficient, 1f, maxStacksFromAttacker);
			}
		}
		if (damageInfo.crit && (ulong)(damageInfo.damageType & DamageType.SuperBleedOnCrit) != 0L)
		{
			DotController.InflictDot(victim, damageInfo.attacker, DotController.DotIndex.SuperBleed, 15f * damageInfo.procCoefficient, 1f, maxStacksFromAttacker);
		}
		if (component2.HasBuff(RoR2Content.Buffs.LifeSteal))
		{
			float amount = damageInfo.damage * 0.2f;
			component2.healthComponent.Heal(amount, damageInfo.procChainMask);
		}
		int itemCount15 = inventory.GetItemCount(RoR2Content.Items.FireballsOnHit);
		if (itemCount15 > 0 && !damageInfo.procChainMask.HasProc(ProcType.Meatball))
		{
			InputBankTest component5 = component2.GetComponent<InputBankTest>();
			Vector3 vector2 = (characterBody.characterMotor ? (victim.transform.position + Vector3.up * (characterBody.characterMotor.capsuleHeight * 0.5f + 2f)) : (victim.transform.position + Vector3.up * 2f));
			Vector3 vector3 = (component5 ? component5.aimDirection : victim.transform.forward);
			vector3 = Vector3.up;
			float num11 = 20f;
			if (Util.CheckRoll(10f * damageInfo.procCoefficient, master))
			{
				EffectData effectData = new EffectData
				{
					scale = 1f,
					origin = vector2
				};
				EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MuzzleFlashes/MuzzleflashFireMeatBall"), effectData, transmit: true);
				int num12 = 3;
				float damageCoefficient11 = 3f * (float)itemCount15;
				float damage5 = Util.OnHitProcDamage(damageInfo.damage, component2.damage, damageCoefficient11);
				float maxInclusive = 30f;
				ProcChainMask procChainMask7 = damageInfo.procChainMask;
				procChainMask7.AddProc(ProcType.Meatball);
				float speedOverride2 = UnityEngine.Random.Range(15f, maxInclusive);
				float num13 = 360 / num12;
				_ = num13 / 360f;
				float num14 = 1f;
				float num15 = num13;
				for (int n = 0; n < num12; n++)
				{
					float num16 = (float)n * MathF.PI * 2f / (float)num12;
					FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
					fireProjectileInfo.projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/FireMeatBall");
					fireProjectileInfo.position = vector2 + new Vector3(num14 * Mathf.Sin(num16), 0f, num14 * Mathf.Cos(num16));
					fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(vector3);
					fireProjectileInfo.procChainMask = procChainMask7;
					fireProjectileInfo.target = victim;
					fireProjectileInfo.owner = component2.gameObject;
					fireProjectileInfo.damage = damage5;
					fireProjectileInfo.crit = damageInfo.crit;
					fireProjectileInfo.force = 200f;
					fireProjectileInfo.damageColorIndex = DamageColorIndex.Item;
					fireProjectileInfo.speedOverride = speedOverride2;
					fireProjectileInfo.useSpeedOverride = true;
					FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
					num15 += num13;
					ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
					vector3.x += Mathf.Sin(num16 + UnityEngine.Random.Range(0f - num11, num11));
					vector3.z += Mathf.Cos(num16 + UnityEngine.Random.Range(0f - num11, num11));
				}
			}
		}
		int itemCount16 = inventory.GetItemCount(RoR2Content.Items.LightningStrikeOnHit);
		if (itemCount16 > 0 && !damageInfo.procChainMask.HasProc(ProcType.LightningStrikeOnHit) && Util.CheckRoll(10f * damageInfo.procCoefficient, master))
		{
			float damageValue6 = Util.OnHitProcDamage(damageInfo.damage, component2.damage, 5f * (float)itemCount16);
			ProcChainMask procChainMask8 = damageInfo.procChainMask;
			procChainMask8.AddProc(ProcType.LightningStrikeOnHit);
			HurtBox target = characterBody.mainHurtBox;
			if ((bool)characterBody.hurtBoxGroup)
			{
				target = characterBody.hurtBoxGroup.hurtBoxes[UnityEngine.Random.Range(0, characterBody.hurtBoxGroup.hurtBoxes.Length)];
			}
			OrbManager.instance.AddOrb(new SimpleLightningStrikeOrb
			{
				attacker = component2.gameObject,
				damageColorIndex = DamageColorIndex.Item,
				damageValue = damageValue6,
				isCrit = Util.CheckRoll(component2.crit, master),
				procChainMask = procChainMask8,
				procCoefficient = 1f,
				target = target
			});
		}
		if ((ulong)(damageInfo.damageType & DamageType.LunarSecondaryRootOnHit) != 0L && (bool)characterBody)
		{
			int itemCount17 = master.inventory.GetItemCount(RoR2Content.Items.LunarSecondaryReplacement);
			characterBody.AddTimedBuff(RoR2Content.Buffs.LunarSecondaryRoot, 3f * (float)itemCount17);
		}
		if ((ulong)(damageInfo.damageType & DamageType.FruitOnHit) != 0L && (bool)characterBody)
		{
			characterBody.AddTimedBuff(RoR2Content.Buffs.Fruiting, 10f);
		}
		if (inventory.GetItemCount(DLC1Content.Items.DroneWeaponsBoost) > 0)
		{
			DroneWeaponsBoostBehavior component6 = component2.GetComponent<DroneWeaponsBoostBehavior>();
			if ((bool)component6)
			{
				component6.OnEnemyHit(damageInfo, characterBody);
			}
		}
		if (victim.tag != "Player" && inventory.GetItemCount(DLC2Content.Items.IncreaseDamageOnMultiKill) > 0)
		{
			component2.AddIncreasedDamageMultiKillTime();
		}
		if (component2.luminousShotReady)
		{
			DamageInfo damageInfo3 = new DamageInfo
			{
				damage = component2.luminousShotDamage,
				damageColorIndex = DamageColorIndex.Luminous,
				damageType = DamageType.Generic,
				attacker = component2.gameObject,
				position = characterBody.transform.position,
				crit = damageInfo.crit,
				force = Vector3.zero,
				inflictor = null,
				procCoefficient = 1f
			};
			characterBody.healthComponent.TakeDamage(damageInfo3);
			EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/IncreasePrimaryDamageImpact"), new EffectData
			{
				origin = characterBody.transform.position
			}, transmit: true);
			component2.IncreasePrimaryDamageReset();
			Util.PlaySound("Play_item_proc_increasePrimaryDamage", characterBody.gameObject);
		}
		if (component2.HasBuff(DLC2Content.Buffs.CookingRolling))
		{
			characterBody.AddBuff(DLC2Content.Buffs.CookingRolled);
		}
		else if (component2.HasBuff(DLC2Content.Buffs.CookingSearing))
		{
			characterBody.AddBuff(DLC2Content.Buffs.CookingRoasted);
		}
		if (characterBody.HasBuff(DLC2Content.Buffs.CookingOiled) && component2.HasBuff(DLC2Content.Buffs.CookingSearing))
		{
			characterBody.AddBuff(DLC2Content.Buffs.CookingFlambe);
			characterBody.RemoveBuff(DLC2Content.Buffs.CookingOiled);
		}
		if (characterBody.HasBuff(DLC2Content.Buffs.CookingFlambe) && (DamageTypeExtended)damageInfo.damageType == DamageTypeExtended.ChefSearDamage)
		{
			float num17 = 0.75f;
			InflictDotInfo inflictDotInfo = default(InflictDotInfo);
			inflictDotInfo.attackerObject = damageInfo.attacker;
			inflictDotInfo.victimObject = victim;
			inflictDotInfo.totalDamage = component2.damage * num17;
			inflictDotInfo.damageMultiplier = 1f;
			inflictDotInfo.dotIndex = DotController.DotIndex.StrongerBurn;
			inflictDotInfo.maxStacksFromAttacker = maxStacksFromAttacker;
			InflictDotInfo dotInfo2 = inflictDotInfo;
			StrengthenBurnUtils.CheckDotForUpgrade(inventory, ref dotInfo2);
			for (int num = 0; num < 3; num++)
			{
				DotController.InflictDot(ref dotInfo2);
			}
		}
		int itemCount18 = inventory.GetItemCount(DLC2Content.Items.MeteorAttackOnHighDamage);
		if (itemCount18 > 0 && !damageInfo.procChainMask.HasProc(ProcType.MeteorAttackOnHighDamage))
		{
			num2 = 0f;
			float num3 = (int)(damageInfo.damage / component2.damage);
			num2 = 5f + 1f * num3 * (float)itemCount18;
			if (num2 > 50f)
			{
				num2 = 50f;
			}
			if (Util.CheckRoll(num2 * damageInfo.procCoefficient, master))
			{
				ProcChainMask procChainMask9 = damageInfo.procChainMask;
				procChainMask9.AddProc(ProcType.MeteorAttackOnHighDamage);
				float num4 = 20f + 0.5f * (float)itemCount18 * num3;
				if (num4 > 75f)
				{
					num4 = 75f;
				}
				num4 *= component2.damage;
				Vector3 vector4 = damageInfo.position;
				num5 = 10f;
				float blastForce = 5000f;
				Vector3 origin = vector4 + Vector3.up * 6f;
				Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
				onUnitSphere.y = -1f;
				if (Physics.Raycast(origin, onUnitSphere, out var hitInfo, 12f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
				{
					vector4 = hitInfo.point;
				}
				else if (Physics.Raycast(vector4, Vector3.down, out hitInfo, float.PositiveInfinity, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
				{
					vector4 = hitInfo.point;
				}
				if (!component2.runicLensMeteorReady)
				{
					Util.PlaySound("Play_item_proc_meteorAttackOnHighDamage", component2.gameObject);
					EffectManager.SpawnEffect(CommonAssets.runicMeteorEffect, new EffectData
					{
						origin = vector4,
						scale = num5
					}, transmit: true);
					new EffectData().origin = vector4;
					component2.runicLensUpdateVariables(num4, vector4, damageInfo, blastForce, num5);
				}
			}
		}
		int itemCount19 = inventory.GetItemCount(DLC2Content.Items.KnockBackHitEnemies);
		if (itemCount19 > 0 && !characterBody.isBoss)
		{
			CharacterMotor component7 = victim.GetComponent<CharacterMotor>();
			victim.GetComponent<RigidbodyMotor>();
			if ((bool)component7 && component7.isGrounded && !characterBody.isChampion && (characterBody.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage) == 0 && !characterBody.HasBuff(DLC2Content.Buffs.KnockUpHitEnemies) && Util.CheckRoll(Util.ConvertAmplificationPercentageIntoReductionPercentage(7.5f * (float)itemCount19 * damageInfo.procCoefficient)))
			{
				Ray aimRay = component2.inputBank.GetAimRay();
				characterBody.AddTimedBuff(DLC2Content.Buffs.KnockUpHitEnemies, 5f);
				float scale = 1f;
				switch (characterBody.hullClassification)
				{
				case HullClassification.Human:
					scale = 1f;
					break;
				case HullClassification.Golem:
					scale = 5f;
					break;
				case HullClassification.BeetleQueen:
					scale = 10f;
					break;
				}
				EffectManager.SpawnEffect(CommonAssets.knockbackFinEffect, new EffectData
				{
					origin = characterBody.gameObject.transform.position,
					scale = scale
				}, transmit: true);
				if (!characterBody.mainHurtBox)
				{
					_ = characterBody.transform;
				}
				else
				{
					_ = characterBody.mainHurtBox.transform;
				}
				Vector3 vector5 = new Vector3(0f, 1f, 0f);
				num6 = component7.mass * 20f;
				float num7 = num6 + num6 / 10f * (float)(itemCount19 - 1);
				component7.ApplyForce(num7 * vector5);
				component7.ApplyForce(num7 / 2f * aimRay.direction);
				Util.PlaySound("Play_item_proc_knockBackHitEnemies", component2.gameObject);
			}
		}
		int itemCount20 = inventory.GetItemCount(DLC2Content.Items.StunAndPierce);
		if (itemCount20 > 0 && !damageInfo.procChainMask.HasProc(ProcType.StunAndPierceDamage) && Util.CheckRoll(15f * damageInfo.procCoefficient, master))
		{
			float damage6 = component2.damage * 0.3f * (float)itemCount20;
			ProcChainMask procChainMask10 = damageInfo.procChainMask;
			procChainMask10.AddProc(ProcType.StunAndPierceDamage);
			Quaternion rotation3 = Quaternion.LookRotation(new Ray(component2.inputBank.aimOrigin, component2.inputBank.aimDirection).direction);
			GameObject projectilePrefab2 = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/StunAndPierceBoomerang");
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.projectilePrefab = projectilePrefab2;
			fireProjectileInfo.crit = component2.RollCrit();
			fireProjectileInfo.damage = damage6;
			fireProjectileInfo.damageColorIndex = DamageColorIndex.Item;
			fireProjectileInfo.force = 0f;
			fireProjectileInfo.owner = component2.gameObject;
			fireProjectileInfo.position = aimOrigin;
			fireProjectileInfo.rotation = rotation3;
			FireProjectileInfo fireProjectileInfo3 = fireProjectileInfo;
			fireProjectileInfo3.procChainMask = procChainMask10;
			ProjectileManager.instance.FireProjectile(fireProjectileInfo3);
		}
		if (characterBody.HasBuff(DLC2Content.Buffs.EliteBeadCorruption) && !component2.HasBuff(DLC2Content.Buffs.EliteBead) && !damageInfo.procChainMask.HasProc(ProcType.Thorns))
		{
			float damageValue7 = component2.maxHealth * 0.025f;
			component2.RollCrit();
			_ = characterBody.teamComponent.teamIndex;
			LightningOrb lightningOrb3 = new LightningOrb();
			lightningOrb3.attacker = characterBody.gameObject;
			lightningOrb3.bouncedObjects = null;
			lightningOrb3.bouncesRemaining = 0;
			lightningOrb3.damageCoefficientPerBounce = 1f;
			lightningOrb3.damageColorIndex = DamageColorIndex.WeakPoint;
			lightningOrb3.damageValue = damageValue7;
			lightningOrb3.isCrit = false;
			lightningOrb3.lightningType = LightningOrb.LightningType.BeadDamage;
			lightningOrb3.origin = characterBody.transform.position;
			lightningOrb3.procChainMask = default(ProcChainMask);
			lightningOrb3.procChainMask.AddProc(ProcType.Thorns);
			lightningOrb3.procCoefficient = 0.3f;
			lightningOrb3.range = 0f;
			lightningOrb3.target = component2.mainHurtBox;
			OrbManager.instance.AddOrb(lightningOrb3);
		}
		if (component2.HasBuff(DLC2Content.Buffs.EliteAurelionite))
		{
			CharacterMaster master2 = component2.master;
			int baseCost = 2;
			int difficultyScaledCost = Run.instance.GetDifficultyScaledCost(baseCost);
			uint money = master2.money;
			master2.money = (uint)Mathf.Max(0f, (float)master2.money - (float)difficultyScaledCost);
			if (money - master2.money != 0)
			{
				GoldOrb goldOrb2 = new GoldOrb();
				goldOrb2.origin = damageInfo.position;
				goldOrb2.target = (component2 ? component2.mainHurtBox : characterBody.mainHurtBox);
				goldOrb2.goldAmount = (uint)difficultyScaledCost + (uint)((float)difficultyScaledCost * 0.2f);
				OrbManager.instance.AddOrb(goldOrb2);
				EffectManager.SimpleImpactEffect(CommonAssets.loseCoinsImpactEffectPrefab, damageInfo.position, Vector3.up, transmit: true);
			}
		}
	}

	private void HandleDamageWithNoAttacker([NotNull] DamageInfo damageInfo, [NotNull] GameObject victim)
	{
		CharacterBody characterBody = (victim ? victim.GetComponent<CharacterBody>() : null);
		if (characterBody == null)
		{
			return;
		}
		if ((ulong)(damageInfo.damageType & DamageType.LunarRuin) != 0)
		{
			if ((ulong)(damageInfo.damageType & DamageTypeExtended.ApplyBuffPermanently) != 0L)
			{
				characterBody.AddTimedBuff(DLC2Content.Buffs.lunarruin, 420f);
			}
			else
			{
				DotController.InflictDot(characterBody.gameObject, damageInfo.attacker, DotController.DotIndex.LunarRuin, 5f);
			}
		}
		if ((ulong)(damageInfo.damageType & DamageTypeExtended.DisableAllSkills) != 0L)
		{
			if ((ulong)(damageInfo.damageType & DamageTypeExtended.ApplyBuffPermanently) != 0L)
			{
				Debug.LogFormat("Adding DisableAllSkills");
				characterBody.AddTimedBuff(DLC2Content.Buffs.DisableAllSkills, 420f);
			}
			else
			{
				Debug.LogFormat("Adding DisableAllSkills for 5s");
				characterBody.AddTimedBuff(DLC2Content.Buffs.DisableAllSkills, 5f);
			}
		}
	}

	public void OnCharacterHitGroundServer(CharacterBody characterBody, CharacterMotor.HitGroundInfo hitGroundInfo)
	{
		bool flag = RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.weakAssKneesArtifactDef);
		float num = Mathf.Abs(hitGroundInfo.velocity.y);
		Inventory inventory = characterBody.inventory;
		_ = characterBody.master;
		CharacterMotor characterMotor = characterBody.characterMotor;
		bool tookFallDamage = false;
		if ((((bool)inventory && inventory.GetItemCount(RoR2Content.Items.FallBoots) != 0) ? 1 : 0) <= (false ? 1 : 0) && (characterBody.bodyFlags & CharacterBody.BodyFlags.IgnoreFallDamage) == 0)
		{
			float num2 = Mathf.Max(num - (characterBody.jumpPower + 20f), 0f);
			if (num2 > 0f)
			{
				HealthComponent component = characterBody.GetComponent<HealthComponent>();
				if ((bool)component)
				{
					tookFallDamage = true;
					float num3 = num2 / 60f;
					DamageInfo damageInfo = new DamageInfo
					{
						attacker = null,
						inflictor = null,
						crit = false,
						damage = num3 * characterBody.maxHealth,
						damageType = (DamageType.NonLethal | DamageType.FallDamage),
						force = Vector3.zero,
						position = characterBody.footPosition,
						procCoefficient = 0f
					};
					if (flag || (characterBody.teamComponent.teamIndex == TeamIndex.Player && Run.instance.selectedDifficulty >= DifficultyIndex.Eclipse3))
					{
						damageInfo.damage *= 2f;
						damageInfo.damageType &= (DamageTypeCombo)(~DamageType.NonLethal);
						damageInfo.damageType |= (DamageTypeCombo)DamageType.BypassOneShotProtection;
					}
					component.TakeDamage(damageInfo);
				}
			}
		}
		OnCharacterHitGroundSFX(characterBody, hitGroundInfo, tookFallDamage);
		characterMotor.CallRpcOnHitGroundSFX(hitGroundInfo, tookFallDamage);
	}

	public void OnCharacterHitGroundSFX(CharacterBody characterBody, CharacterMotor.HitGroundInfo hitGroundInfo, bool tookFallDamage)
	{
		CharacterMotor characterMotor = characterBody.characterMotor;
		if (!characterMotor || (!hitGroundInfo.isValidForEffect && !(Run.FixedTimeStamp.now - characterMotor.lastGroundedTime > 0.2f)))
		{
			return;
		}
		Vector3 footPosition = characterBody.footPosition;
		float radius = characterBody.radius;
		if (!Physics.Raycast(new Ray(footPosition + Vector3.up * 1.5f, Vector3.down), out var hitInfo, 4f, (int)LayerIndex.world.mask | (int)LayerIndex.water.mask, QueryTriggerInteraction.Collide))
		{
			return;
		}
		SurfaceDef objectSurfaceDef = SurfaceDefProvider.GetObjectSurfaceDef(hitInfo.collider, hitInfo.point);
		if (!objectSurfaceDef)
		{
			return;
		}
		if (NetworkServer.active)
		{
			EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/CharacterLandImpact"), new EffectData
			{
				origin = footPosition,
				scale = radius,
				color = objectSurfaceDef.approximateColor
			}, transmit: true);
		}
		if ((bool)objectSurfaceDef.footstepEffectPrefab)
		{
			EffectManager.SpawnEffect(objectSurfaceDef.footstepEffectPrefab, new EffectData
			{
				origin = hitInfo.point,
				scale = radius * 3f
			}, transmit: false);
		}
		SfxLocator component = characterBody.GetComponent<SfxLocator>();
		if ((bool)component)
		{
			if (objectSurfaceDef.materialSwitchString != null && objectSurfaceDef.materialSwitchString.Length > 0)
			{
				AkSoundEngine.SetSwitch("material", objectSurfaceDef.materialSwitchString, characterBody.gameObject);
			}
			else
			{
				AkSoundEngine.SetSwitch("material", "dirt", characterBody.gameObject);
			}
			Util.PlaySound(component.landingSound, characterBody.gameObject);
			if (tookFallDamage)
			{
				Util.PlaySound(component.fallDamageSound, characterBody.gameObject);
			}
		}
	}

	[Obsolete("Use OnCharacterHitGroundServer instead, which this is just a backwards-compatibility wrapper for.", false)]
	public void OnCharacterHitGround(CharacterBody characterBody, Vector3 impactVelocity)
	{
		OnCharacterHitGroundServer(characterBody, new CharacterMotor.HitGroundInfo
		{
			velocity = impactVelocity
		});
	}

	private void OnPlayerCharacterDeath(DamageReport damageReport, NetworkUser victimNetworkUser)
	{
		if ((bool)victimNetworkUser)
		{
			CharacterBody victimBody = damageReport.victimBody;
			string text = (((ulong)(damageReport.damageInfo.damageType & DamageType.VoidDeath) != 0L) ? "PLAYER_DEATH_QUOTE_VOIDDEATH" : (damageReport.isFallDamage ? fallDamageDeathQuoteTokens[UnityEngine.Random.Range(0, fallDamageDeathQuoteTokens.Length)] : ((!victimBody || !victimBody.inventory || victimBody.inventory.GetItemCount(RoR2Content.Items.LunarDagger) <= 0) ? standardDeathQuoteTokens[UnityEngine.Random.Range(0, standardDeathQuoteTokens.Length)] : "PLAYER_DEATH_QUOTE_BRITTLEDEATH")));
			if ((bool)victimNetworkUser.masterController)
			{
				victimNetworkUser.masterController.finalMessageTokenServer = text;
			}
			Chat.SendBroadcastChat(new Chat.PlayerDeathChatMessage
			{
				subjectAsNetworkUser = victimNetworkUser,
				baseToken = text
			});
		}
	}

	private static void ProcIgniteOnKill(DamageReport damageReport, int igniteOnKillCount, CharacterBody victimBody, TeamIndex attackerTeamIndex)
	{
		float num = 8f + 4f * (float)igniteOnKillCount;
		float radius = victimBody.radius;
		float num2 = num + radius;
		float num3 = 1.5f;
		float baseDamage = damageReport.attackerBody.damage * num3;
		Vector3 corePosition = victimBody.corePosition;
		igniteOnKillSphereSearch.origin = corePosition;
		igniteOnKillSphereSearch.mask = LayerIndex.entityPrecise.mask;
		igniteOnKillSphereSearch.radius = num2;
		igniteOnKillSphereSearch.RefreshCandidates();
		igniteOnKillSphereSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(attackerTeamIndex));
		igniteOnKillSphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
		igniteOnKillSphereSearch.OrderCandidatesByDistance();
		igniteOnKillSphereSearch.GetHurtBoxes(igniteOnKillHurtBoxBuffer);
		igniteOnKillSphereSearch.ClearCandidates();
		float value = (float)(1 + igniteOnKillCount) * 0.75f * damageReport.attackerBody.damage;
		for (int i = 0; i < igniteOnKillHurtBoxBuffer.Count; i++)
		{
			HurtBox hurtBox = igniteOnKillHurtBoxBuffer[i];
			if ((bool)hurtBox.healthComponent)
			{
				InflictDotInfo inflictDotInfo = default(InflictDotInfo);
				inflictDotInfo.victimObject = hurtBox.healthComponent.gameObject;
				inflictDotInfo.attackerObject = damageReport.attacker;
				inflictDotInfo.totalDamage = value;
				inflictDotInfo.dotIndex = DotController.DotIndex.Burn;
				inflictDotInfo.damageMultiplier = 1f;
				InflictDotInfo dotInfo = inflictDotInfo;
				if ((bool)damageReport?.attackerMaster?.inventory)
				{
					StrengthenBurnUtils.CheckDotForUpgrade(damageReport.attackerMaster.inventory, ref dotInfo);
				}
				DotController.InflictDot(ref dotInfo);
			}
		}
		igniteOnKillHurtBoxBuffer.Clear();
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.radius = num2;
		blastAttack.baseDamage = baseDamage;
		blastAttack.procCoefficient = 0f;
		blastAttack.crit = Util.CheckRoll(damageReport.attackerBody.crit, damageReport.attackerMaster);
		blastAttack.damageColorIndex = DamageColorIndex.Item;
		blastAttack.attackerFiltering = AttackerFiltering.Default;
		blastAttack.falloffModel = BlastAttack.FalloffModel.None;
		blastAttack.attacker = damageReport.attacker;
		blastAttack.teamIndex = attackerTeamIndex;
		blastAttack.position = corePosition;
		blastAttack.Fire();
		EffectManager.SpawnEffect(CommonAssets.igniteOnKillExplosionEffectPrefab, new EffectData
		{
			origin = corePosition,
			scale = num2,
			rotation = Util.QuaternionSafeLookRotation(damageReport.damageInfo.force)
		}, transmit: true);
	}

	public void OnCharacterDeath(DamageReport damageReport)
	{
		if (!NetworkServer.active || damageReport == null)
		{
			return;
		}
		DamageInfo damageInfo = damageReport.damageInfo;
		GameObject gameObject = null;
		if ((bool)damageReport.victim)
		{
			gameObject = damageReport.victim.gameObject;
		}
		CharacterBody victimBody = damageReport.victimBody;
		TeamComponent teamComponent = null;
		CharacterMaster victimMaster = damageReport.victimMaster;
		TeamIndex teamIndex = damageReport.victimTeamIndex;
		Vector3 vector = Vector3.zero;
		Quaternion quaternion = Quaternion.identity;
		Vector3 vector2 = Vector3.zero;
		Transform transform = gameObject.transform;
		if ((bool)transform)
		{
			vector = transform.position;
			quaternion = transform.rotation;
			vector2 = vector;
		}
		InputBankTest inputBankTest = null;
		EquipmentIndex equipmentIndex = EquipmentIndex.None;
		EquipmentDef equipmentDef = null;
		if ((bool)victimBody)
		{
			teamComponent = victimBody.teamComponent;
			inputBankTest = victimBody.inputBank;
			vector2 = victimBody.corePosition;
			if ((bool)victimBody.equipmentSlot)
			{
				equipmentIndex = victimBody.equipmentSlot.equipmentIndex;
				equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
			}
		}
		Ray ray = (inputBankTest ? inputBankTest.GetAimRay() : new Ray(vector, quaternion * Vector3.forward));
		GameObject attacker = damageReport.attacker;
		CharacterBody attackerBody = damageReport.attackerBody;
		CharacterMaster attackerMaster = damageReport.attackerMaster;
		Inventory inventory = (attackerMaster ? attackerMaster.inventory : null);
		TeamIndex attackerTeamIndex = damageReport.attackerTeamIndex;
		if ((bool)teamComponent)
		{
			teamIndex = teamComponent.teamIndex;
		}
		if ((bool)victimBody && (bool)victimMaster)
		{
			PlayerCharacterMasterController playerCharacterMasterController = victimMaster.playerCharacterMasterController;
			if ((bool)playerCharacterMasterController)
			{
				NetworkUser networkUser = playerCharacterMasterController.networkUser;
				if ((bool)networkUser)
				{
					OnPlayerCharacterDeath(damageReport, networkUser);
				}
			}
			if (victimBody.HasBuff(RoR2Content.Buffs.AffixWhite))
			{
				Vector3 position = vector2;
				GameObject gameObject2 = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/GenericDelayBlast"), position, Quaternion.identity);
				float num = 12f + victimBody.radius;
				gameObject2.transform.localScale = new Vector3(num, num, num);
				DelayBlast component = gameObject2.GetComponent<DelayBlast>();
				if ((bool)component)
				{
					component.position = position;
					component.baseDamage = victimBody.damage * 1.5f;
					component.baseForce = 2300f;
					component.attacker = gameObject;
					component.radius = num;
					component.crit = Util.CheckRoll(victimBody.crit, victimMaster);
					component.procCoefficient = 0.75f;
					component.maxTimer = 2f;
					component.falloffModel = BlastAttack.FalloffModel.None;
					component.explosionEffect = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/AffixWhiteExplosion");
					component.delayEffect = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/AffixWhiteDelayEffect");
					component.damageType = DamageType.Freeze2s;
					TeamFilter component2 = gameObject2.GetComponent<TeamFilter>();
					if ((bool)component2)
					{
						component2.teamIndex = TeamComponent.GetObjectTeam(component.attacker);
					}
				}
			}
			if (victimBody.HasBuff(RoR2Content.Buffs.AffixPoison))
			{
				Vector3 position2 = vector2;
				Quaternion rotation = Quaternion.LookRotation(ray.direction);
				GameObject gameObject3 = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterMasters/UrchinTurretMaster"), position2, rotation);
				CharacterMaster component3 = gameObject3.GetComponent<CharacterMaster>();
				if ((bool)component3)
				{
					component3.teamIndex = teamIndex;
					NetworkServer.Spawn(gameObject3);
					component3.SpawnBodyHere();
				}
			}
			if (RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.wispOnDeath) && teamIndex == TeamIndex.Monster && victimMaster.masterIndex != CommonAssets.wispSoulMasterPrefabMasterComponent.masterIndex)
			{
				MasterSummon masterSummon = new MasterSummon();
				masterSummon.position = vector2;
				masterSummon.ignoreTeamMemberLimit = true;
				masterSummon.masterPrefab = CommonAssets.wispSoulMasterPrefabMasterComponent.gameObject;
				masterSummon.summonerBodyObject = gameObject;
				masterSummon.rotation = Quaternion.LookRotation(ray.direction);
				masterSummon.Perform();
			}
			if (victimBody.HasBuff(RoR2Content.Buffs.Fruiting) || (damageReport.damageInfo != null && (ulong)(damageReport.damageInfo.damageType & DamageType.FruitOnHit) != 0))
			{
				EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/TreebotFruitDeathEffect.prefab"), new EffectData
				{
					origin = vector,
					rotation = UnityEngine.Random.rotation
				}, transmit: true);
				int num2 = Mathf.Min(Math.Max(1, (int)(victimBody.bestFitRadius * 2f)), 8);
				GameObject original = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/TreebotFruitPack");
				for (int i = 0; i < num2; i++)
				{
					GameObject obj = UnityEngine.Object.Instantiate(original, vector + UnityEngine.Random.insideUnitSphere * victimBody.radius * 0.5f, UnityEngine.Random.rotation);
					TeamFilter component4 = obj.GetComponent<TeamFilter>();
					if ((bool)component4)
					{
						component4.teamIndex = attackerTeamIndex;
					}
					obj.GetComponentInChildren<HealthPickup>();
					obj.transform.localScale = new Vector3(1f, 1f, 1f);
					NetworkServer.Spawn(obj);
				}
			}
			if (victimBody.HasBuff(DLC1Content.Buffs.EliteEarth))
			{
				MasterSummon masterSummon2 = new MasterSummon();
				masterSummon2.position = vector2;
				masterSummon2.ignoreTeamMemberLimit = true;
				masterSummon2.masterPrefab = CommonAssets.eliteEarthHealerMaster;
				masterSummon2.summonerBodyObject = gameObject;
				masterSummon2.rotation = Quaternion.LookRotation(ray.direction);
				masterSummon2.Perform();
			}
			if (victimBody.HasBuff(DLC1Content.Buffs.EliteVoid) && (!victimMaster || victimMaster.IsDeadAndOutOfLivesServer()))
			{
				Vector3 position3 = vector2;
				GameObject gameObject4 = UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/EliteVoid/VoidInfestorMaster.prefab").WaitForCompletion(), position3, Quaternion.identity);
				CharacterMaster component5 = gameObject4.GetComponent<CharacterMaster>();
				if ((bool)component5)
				{
					component5.teamIndex = TeamIndex.Void;
					NetworkServer.Spawn(gameObject4);
					component5.SpawnBodyHere();
				}
			}
		}
		if ((bool)attackerBody)
		{
			attackerBody.HandleOnKillEffectsServer(damageReport);
			if ((bool)attackerMaster && (bool)inventory)
			{
				int itemCount = inventory.GetItemCount(RoR2Content.Items.IgniteOnKill);
				if (itemCount > 0)
				{
					ProcIgniteOnKill(damageReport, itemCount, victimBody, attackerTeamIndex);
				}
				int itemCount2 = inventory.GetItemCount(RoR2Content.Items.ExplodeOnDeath);
				if (itemCount2 > 0)
				{
					Vector3 position4 = vector2;
					float damageCoefficient = 3.5f * (1f + (float)(itemCount2 - 1) * 0.8f);
					float baseDamage = Util.OnKillProcDamage(attackerBody.damage, damageCoefficient);
					GameObject obj2 = UnityEngine.Object.Instantiate(CommonAssets.explodeOnDeathPrefab, position4, Quaternion.identity);
					DelayBlast component6 = obj2.GetComponent<DelayBlast>();
					if ((bool)component6)
					{
						component6.position = position4;
						component6.baseDamage = baseDamage;
						component6.baseForce = 2000f;
						component6.bonusForce = Vector3.up * 1000f;
						component6.radius = 12f + 2.4f * ((float)itemCount2 - 1f);
						component6.attacker = damageInfo.attacker;
						component6.inflictor = null;
						component6.crit = Util.CheckRoll(attackerBody.crit, attackerMaster);
						component6.maxTimer = 0.5f;
						component6.damageColorIndex = DamageColorIndex.Item;
						component6.falloffModel = BlastAttack.FalloffModel.SweetSpot;
					}
					TeamFilter component7 = obj2.GetComponent<TeamFilter>();
					if ((bool)component7)
					{
						component7.teamIndex = attackerTeamIndex;
					}
					NetworkServer.Spawn(obj2);
				}
				int itemCount3 = inventory.GetItemCount(RoR2Content.Items.Dagger);
				if (itemCount3 > 0)
				{
					float damageCoefficient2 = 1.5f * (float)itemCount3;
					Vector3 vector3 = vector + Vector3.up * 1.8f;
					for (int j = 0; j < 3; j++)
					{
						ProjectileManager.instance.FireProjectile(CommonAssets.daggerPrefab, vector3 + UnityEngine.Random.insideUnitSphere * 0.5f, Util.QuaternionSafeLookRotation(Vector3.up + UnityEngine.Random.insideUnitSphere * 0.1f), attackerBody.gameObject, Util.OnKillProcDamage(attackerBody.damage, damageCoefficient2), 200f, Util.CheckRoll(attackerBody.crit, attackerMaster), DamageColorIndex.Item);
					}
				}
				int itemCount4 = inventory.GetItemCount(RoR2Content.Items.Tooth);
				if (itemCount4 > 0)
				{
					float num3 = Mathf.Pow(itemCount4, 0.25f);
					GameObject obj3 = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/HealPack"), vector, UnityEngine.Random.rotation);
					TeamFilter component8 = obj3.GetComponent<TeamFilter>();
					if ((bool)component8)
					{
						component8.teamIndex = attackerTeamIndex;
					}
					HealthPickup componentInChildren = obj3.GetComponentInChildren<HealthPickup>();
					if ((bool)componentInChildren)
					{
						componentInChildren.flatHealing = 8f;
						componentInChildren.fractionalHealing = 0.02f * (float)itemCount4;
					}
					obj3.transform.localScale = new Vector3(num3, num3, num3);
					NetworkServer.Spawn(obj3);
				}
				float num4 = 0f;
				float num5 = 0f;
				float num6 = 0f;
				float num7 = 0f;
				if (victimBody.HasBuff(DLC2Content.Buffs.CookingChopped))
				{
					num4 += 1f;
					num5 += 2f;
					num6 += 0.04f;
					num7 += 0.75f;
				}
				if (victimBody.HasBuff(DLC2Content.Buffs.CookingOiled))
				{
					num4 += 1f;
					num5 += 2f;
					num6 += 0.04f;
					num7 += 0.75f;
				}
				if (victimBody.HasBuff(DLC2Content.Buffs.CookingRoasted))
				{
					num4 += 1f;
					num5 += 2f;
					num6 += 0.04f;
					num7 += 0.75f;
				}
				if (victimBody.HasBuff(DLC2Content.Buffs.CookingRolled))
				{
					num4 += 1f;
					num5 += 2f;
					num6 += 0.04f;
					num7 += 0.75f;
				}
				if (victimBody.HasBuff(DLC2Content.Buffs.CookingFlambe))
				{
					num4 += 1f;
					num5 += 2f;
					num6 += 0.04f;
					num7 += 0.75f;
				}
				if (num4 > 1f)
				{
					GameObject obj4 = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Chef/ChefFoodPickup"), vector, UnityEngine.Random.rotation);
					TeamFilter component9 = obj4.GetComponent<TeamFilter>();
					if ((bool)component9)
					{
						component9.teamIndex = attackerTeamIndex;
					}
					HealthPickup componentInChildren2 = obj4.GetComponentInChildren<HealthPickup>();
					if ((bool)componentInChildren2)
					{
						componentInChildren2.flatHealing = num5;
						componentInChildren2.fractionalHealing = num6;
					}
					obj4.transform.localScale = new Vector3(num7, num7, num7);
					NetworkServer.Spawn(obj4);
				}
				int itemCount5 = inventory.GetItemCount(RoR2Content.Items.Infusion);
				if (itemCount5 > 0)
				{
					int num8 = itemCount5 * 100;
					if (inventory.infusionBonus < num8)
					{
						InfusionOrb infusionOrb = new InfusionOrb();
						infusionOrb.origin = vector;
						infusionOrb.target = Util.FindBodyMainHurtBox(attackerBody);
						infusionOrb.maxHpValue = itemCount5;
						OrbManager.instance.AddOrb(infusionOrb);
					}
				}
				if ((DamageType)(damageInfo.damageType & DamageType.ResetCooldownsOnKill) == DamageType.ResetCooldownsOnKill)
				{
					EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/Bandit2ResetEffect"), new EffectData
					{
						origin = damageInfo.position
					}, transmit: true);
					SkillLocator skillLocator = attackerBody.skillLocator;
					if ((bool)skillLocator)
					{
						skillLocator.ResetSkills();
					}
				}
				if ((DamageType)(damageInfo.damageType & DamageType.GiveSkullOnKill) == DamageType.GiveSkullOnKill && (bool)victimMaster)
				{
					EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/Bandit2KillEffect"), new EffectData
					{
						origin = damageInfo.position
					}, transmit: true);
					attackerBody.AddBuff(RoR2Content.Buffs.BanditSkull);
				}
				int itemCount6 = inventory.GetItemCount(RoR2Content.Items.Talisman);
				if (itemCount6 > 0 && (bool)attackerBody.equipmentSlot)
				{
					inventory.DeductActiveEquipmentCooldown(2f + (float)itemCount6 * 2f);
				}
				int itemCount7 = inventory.GetItemCount(JunkContent.Items.TempestOnKill);
				if (itemCount7 > 0 && Util.CheckRoll(25f, attackerMaster))
				{
					GameObject obj5 = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/TempestWard"), victimBody.footPosition, Quaternion.identity);
					TeamFilter component10 = obj5.GetComponent<TeamFilter>();
					if ((bool)component10)
					{
						component10.teamIndex = attackerTeamIndex;
					}
					BuffWard component11 = obj5.GetComponent<BuffWard>();
					if ((bool)component11)
					{
						component11.expireDuration = 2f + 6f * (float)itemCount7;
					}
					NetworkServer.Spawn(obj5);
				}
				int itemCount8 = inventory.GetItemCount(RoR2Content.Items.Bandolier);
				if (itemCount8 > 0 && Util.CheckRoll((1f - 1f / Mathf.Pow(itemCount8 + 1, 0.33f)) * 100f, attackerMaster))
				{
					GameObject obj6 = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/AmmoPack"), vector, UnityEngine.Random.rotation);
					TeamFilter component12 = obj6.GetComponent<TeamFilter>();
					if ((bool)component12)
					{
						component12.teamIndex = attackerTeamIndex;
					}
					NetworkServer.Spawn(obj6);
				}
				if ((bool)victimBody && damageReport.victimIsElite)
				{
					int itemCount9 = inventory.GetItemCount(RoR2Content.Items.HeadHunter);
					int itemCount10 = inventory.GetItemCount(RoR2Content.Items.KillEliteFrenzy);
					if (itemCount9 > 0)
					{
						float duration = 3f + 5f * (float)itemCount9;
						for (int k = 0; k < BuffCatalog.eliteBuffIndices.Length; k++)
						{
							BuffIndex buffIndex = BuffCatalog.eliteBuffIndices[k];
							if (victimBody.HasBuff(buffIndex))
							{
								attackerBody.AddTimedBuff(buffIndex, duration);
							}
						}
					}
					if (itemCount10 > 0)
					{
						attackerBody.AddTimedBuff(RoR2Content.Buffs.NoCooldowns, (float)itemCount10 * 4f);
					}
				}
				int itemCount11 = inventory.GetItemCount(RoR2Content.Items.GhostOnKill);
				if (itemCount11 > 0 && (bool)victimBody && Util.CheckRoll(7f, attackerMaster))
				{
					Util.TryToCreateGhost(victimBody, attackerBody, itemCount11 * 30);
				}
				if (inventory.GetItemCount(DLC1Content.Items.MinorConstructOnKill) > 0 && (bool)victimBody && victimBody.isElite && !attackerMaster.IsDeployableLimited(DeployableSlot.MinorConstructOnKill))
				{
					Vector3 forward = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up) * Quaternion.AngleAxis(-80f, Vector3.right) * Vector3.forward;
					FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
					fireProjectileInfo.projectilePrefab = CommonAssets.minorConstructOnKillProjectile;
					fireProjectileInfo.position = vector;
					fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(forward);
					fireProjectileInfo.procChainMask = default(ProcChainMask);
					fireProjectileInfo.target = gameObject;
					fireProjectileInfo.owner = attackerBody.gameObject;
					fireProjectileInfo.damage = 0f;
					fireProjectileInfo.crit = false;
					fireProjectileInfo.force = 0f;
					fireProjectileInfo.damageColorIndex = DamageColorIndex.Item;
					FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
					ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
				}
				int itemCount12 = inventory.GetItemCount(DLC1Content.Items.MoveSpeedOnKill);
				if (itemCount12 > 0)
				{
					int num9 = itemCount12 - 1;
					int num10 = 5;
					float num11 = 1f + (float)num9 * 0.5f;
					attackerBody.ClearTimedBuffs(DLC1Content.Buffs.KillMoveSpeed);
					for (int l = 0; l < num10; l++)
					{
						attackerBody.AddTimedBuff(DLC1Content.Buffs.KillMoveSpeed, num11 * (float)(l + 1) / (float)num10);
					}
					EffectData effectData = new EffectData();
					effectData.origin = attackerBody.corePosition;
					CharacterMotor characterMotor = attackerBody.characterMotor;
					bool flag = false;
					if ((bool)characterMotor)
					{
						Vector3 moveDirection = characterMotor.moveDirection;
						if (moveDirection != Vector3.zero)
						{
							effectData.rotation = Util.QuaternionSafeLookRotation(moveDirection);
							flag = true;
						}
					}
					if (!flag)
					{
						effectData.rotation = attackerBody.transform.rotation;
					}
					EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MoveSpeedOnKillActivate"), effectData, transmit: true);
				}
				if ((bool)equipmentDef && Util.CheckRoll(equipmentDef.dropOnDeathChance * 100f, attackerMaster) && (bool)victimBody)
				{
					PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(equipmentIndex), vector + Vector3.up * 1.5f, Vector3.up * 20f + ray.direction * 2f);
				}
				int itemCount13 = inventory.GetItemCount(RoR2Content.Items.BarrierOnKill);
				if (itemCount13 > 0 && (bool)attackerBody.healthComponent)
				{
					attackerBody.healthComponent.AddBarrier(15f * (float)itemCount13);
				}
				int itemCount14 = inventory.GetItemCount(RoR2Content.Items.BonusGoldPackOnKill);
				if (itemCount14 > 0 && Util.CheckRoll(4f * (float)itemCount14, attackerMaster))
				{
					GameObject obj7 = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/BonusMoneyPack"), vector, UnityEngine.Random.rotation);
					TeamFilter component13 = obj7.GetComponent<TeamFilter>();
					if ((bool)component13)
					{
						component13.teamIndex = attackerTeamIndex;
					}
					NetworkServer.Spawn(obj7);
				}
				int itemCount15 = inventory.GetItemCount(RoR2Content.Items.Plant);
				if (itemCount15 > 0)
				{
					GameObject obj8 = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/InterstellarDeskPlant"), victimBody.footPosition, Quaternion.identity);
					DeskPlantController component14 = obj8.GetComponent<DeskPlantController>();
					if ((bool)component14)
					{
						if ((bool)component14.teamFilter)
						{
							component14.teamFilter.teamIndex = attackerTeamIndex;
						}
						component14.itemCount = itemCount15;
					}
					NetworkServer.Spawn(obj8);
				}
				int incubatorOnKillCount = attackerMaster.inventory.GetItemCount(JunkContent.Items.Incubator);
				if (incubatorOnKillCount > 0 && attackerMaster.GetDeployableCount(DeployableSlot.ParentPodAlly) + attackerMaster.GetDeployableCount(DeployableSlot.ParentAlly) < incubatorOnKillCount && Util.CheckRoll(7f + 1f * (float)incubatorOnKillCount, attackerMaster))
				{
					DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/CharacterSpawnCards/cscParentPod"), new DirectorPlacementRule
					{
						placementMode = DirectorPlacementRule.PlacementMode.Approximate,
						minDistance = 3f,
						maxDistance = 20f,
						spawnOnTarget = transform
					}, RoR2Application.rng);
					directorSpawnRequest.summonerBodyObject = attacker;
					directorSpawnRequest.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest.onSpawnedServer, (Action<SpawnCard.SpawnResult>)delegate(SpawnCard.SpawnResult spawnResult)
					{
						if (spawnResult.success)
						{
							Inventory inventory2 = spawnResult.spawnedInstance.GetComponent<CharacterMaster>().inventory;
							if ((bool)inventory2)
							{
								inventory2.GiveItem(RoR2Content.Items.BoostDamage, 30);
								inventory2.GiveItem(RoR2Content.Items.BoostHp, 10 * incubatorOnKillCount);
							}
						}
					});
					DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
				}
				int itemCount16 = inventory.GetItemCount(RoR2Content.Items.BleedOnHitAndExplode);
				if (itemCount16 > 0 && (bool)victimBody && (victimBody.HasBuff(RoR2Content.Buffs.Bleeding) || victimBody.HasBuff(RoR2Content.Buffs.SuperBleed)))
				{
					Util.PlaySound("Play_bleedOnCritAndExplode_explode", gameObject);
					Vector3 position5 = vector2;
					float damageCoefficient3 = 4f * (float)(1 + (itemCount16 - 1));
					float num12 = 0.15f * (float)(1 + (itemCount16 - 1));
					float baseDamage2 = Util.OnKillProcDamage(attackerBody.damage, damageCoefficient3) + victimBody.maxHealth * num12;
					GameObject obj9 = UnityEngine.Object.Instantiate(CommonAssets.bleedOnHitAndExplodeBlastEffect, position5, Quaternion.identity);
					DelayBlast component15 = obj9.GetComponent<DelayBlast>();
					component15.position = position5;
					component15.baseDamage = baseDamage2;
					component15.baseForce = 0f;
					component15.radius = 16f;
					component15.attacker = damageInfo.attacker;
					component15.inflictor = null;
					component15.crit = Util.CheckRoll(attackerBody.crit, attackerMaster);
					component15.maxTimer = 0f;
					component15.damageColorIndex = DamageColorIndex.Item;
					component15.falloffModel = BlastAttack.FalloffModel.SweetSpot;
					obj9.GetComponent<TeamFilter>().teamIndex = attackerTeamIndex;
					NetworkServer.Spawn(obj9);
				}
				if (attackerMaster.inventory.GetItemCount(DLC2Content.Items.ResetChests) > 0)
				{
					int itemCount17 = attackerMaster.inventory.GetItemCount(DLC2Content.Items.ResetChests);
					if (victimBody.isChampion && Util.CheckRoll(100f, attackerMaster) && (bool)victimBody)
					{
						PickupDropletController.CreatePickupDroplet(CommonAssets.dtSonorousEchoPath.GenerateDrop(Run.instance.runRNG), vector + Vector3.up * 1.5f, Vector3.up * 20f + ray.direction * 2f);
					}
					if (victimBody.isElite && Util.CheckRoll(2f + 1f * (float)itemCount17, attackerMaster) && (bool)victimBody)
					{
						PickupDropletController.CreatePickupDroplet(CommonAssets.dtSonorousEchoPath.GenerateDrop(Run.instance.runRNG), vector + Vector3.up * 1.5f, Vector3.up * 20f + ray.direction * 2f);
					}
				}
			}
		}
		GlobalEventManager.onCharacterDeathGlobal?.Invoke(damageReport);
	}

	public void OnHitAll(DamageInfo damageInfo, GameObject hitObject)
	{
		OnHitAllProcess(damageInfo, hitObject);
	}

	private void OnHitAllProcess(DamageInfo damageInfo, GameObject hitObject)
	{
		if (damageInfo.procCoefficient == 0f || damageInfo.rejected)
		{
			return;
		}
		_ = NetworkServer.active;
		if (!damageInfo.attacker)
		{
			return;
		}
		CharacterBody component = damageInfo.attacker.GetComponent<CharacterBody>();
		if (!component)
		{
			return;
		}
		CharacterMaster master = component.master;
		if (!master)
		{
			return;
		}
		Inventory inventory = master.inventory;
		if (!master.inventory)
		{
			return;
		}
		if (!damageInfo.procChainMask.HasProc(ProcType.Behemoth))
		{
			int itemCount = inventory.GetItemCount(RoR2Content.Items.Behemoth);
			if (itemCount > 0 && damageInfo.procCoefficient != 0f)
			{
				float num = (1.5f + 2.5f * (float)itemCount) * damageInfo.procCoefficient;
				float damageCoefficient = 0.6f;
				float baseDamage = Util.OnHitProcDamage(damageInfo.damage, component.damage, damageCoefficient);
				EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OmniEffect/OmniExplosionVFXQuick"), new EffectData
				{
					origin = damageInfo.position,
					scale = num,
					rotation = Util.QuaternionSafeLookRotation(damageInfo.force)
				}, transmit: true);
				BlastAttack obj = new BlastAttack
				{
					position = damageInfo.position,
					baseDamage = baseDamage,
					baseForce = 0f,
					radius = num,
					attacker = damageInfo.attacker,
					inflictor = null
				};
				obj.teamIndex = TeamComponent.GetObjectTeam(obj.attacker);
				obj.crit = damageInfo.crit;
				obj.procChainMask = damageInfo.procChainMask;
				obj.procCoefficient = 0f;
				obj.damageColorIndex = DamageColorIndex.Item;
				obj.falloffModel = BlastAttack.FalloffModel.None;
				obj.damageType = damageInfo.damageType;
				obj.Fire();
			}
		}
		if ((component.HasBuff(RoR2Content.Buffs.AffixBlue) ? 1 : 0) > 0)
		{
			float damageCoefficient2 = 0.5f;
			float damage = Util.OnHitProcDamage(damageInfo.damage, component.damage, damageCoefficient2);
			float force = 0f;
			Vector3 position = damageInfo.position;
			ProjectileManager.instance.FireProjectile(LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LightningStake"), position, Quaternion.identity, damageInfo.attacker, damage, force, damageInfo.crit, DamageColorIndex.Item);
		}
	}

	public void OnCrit(CharacterBody body, DamageInfo damageInfo, CharacterMaster master, float procCoefficient, ProcChainMask procChainMask)
	{
		Vector3 hitPos = body.corePosition;
		GameObject gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/Critspark");
		if ((bool)body)
		{
			if (body.critMultiplier > 2f)
			{
				gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/CritsparkHeavy");
			}
			if ((bool)body && procCoefficient > 0f && (bool)master && (bool)master.inventory)
			{
				Inventory inventory = master.inventory;
				if (!procChainMask.HasProc(ProcType.HealOnCrit))
				{
					procChainMask.AddProc(ProcType.HealOnCrit);
					int itemCount = inventory.GetItemCount(RoR2Content.Items.HealOnCrit);
					if (itemCount > 0 && (bool)body.healthComponent)
					{
						Util.PlaySound("Play_item_proc_crit_heal", body.gameObject);
						if (NetworkServer.active)
						{
							body.healthComponent.Heal((4f + (float)itemCount * 4f) * procCoefficient, procChainMask);
						}
					}
				}
				if (inventory.GetItemCount(RoR2Content.Items.AttackSpeedOnCrit) > 0)
				{
					body.AddTimedBuff(RoR2Content.Buffs.AttackSpeedOnCrit, 3f * procCoefficient);
				}
				int itemCount2 = inventory.GetItemCount(JunkContent.Items.CooldownOnCrit);
				if (itemCount2 > 0)
				{
					Util.PlaySound("Play_item_proc_crit_cooldown", body.gameObject);
					SkillLocator component = body.GetComponent<SkillLocator>();
					if ((bool)component)
					{
						float dt = (float)itemCount2 * procCoefficient;
						if ((bool)component.primary)
						{
							component.primary.RunRecharge(dt);
						}
						if ((bool)component.secondary)
						{
							component.secondary.RunRecharge(dt);
						}
						if ((bool)component.utility)
						{
							component.utility.RunRecharge(dt);
						}
						if ((bool)component.special)
						{
							component.special.RunRecharge(dt);
						}
					}
				}
			}
		}
		if (damageInfo != null)
		{
			hitPos = damageInfo.position;
		}
		if ((bool)gameObject)
		{
			EffectManager.SimpleImpactEffect(gameObject, hitPos, Vector3.up, transmit: true);
		}
	}

	public static void OnTeamLevelUp(TeamIndex teamIndex)
	{
		GlobalEventManager.onTeamLevelUp?.Invoke(teamIndex);
	}

	public static void OnCharacterLevelUp(CharacterBody characterBody)
	{
		GlobalEventManager.onCharacterLevelUp?.Invoke(characterBody);
	}

	public void OnInteractionBegin(Interactor interactor, IInteractable interactable, GameObject interactableObject)
	{
		if (!interactor)
		{
			Debug.LogError("OnInteractionBegin invalid interactor!");
			return;
		}
		if (interactable == null)
		{
			Debug.LogError("OnInteractionBegin invalid interactable!");
			return;
		}
		if (!interactableObject)
		{
			Debug.LogError("OnInteractionBegin invalid interactableObject!");
			return;
		}
		GlobalEventManager.OnInteractionsGlobal?.Invoke(interactor, interactable, interactableObject);
		CharacterBody component = interactor.GetComponent<CharacterBody>();
		Vector3 vector = Vector3.zero;
		Quaternion rotation = Quaternion.identity;
		Transform transform = interactableObject.transform;
		if ((bool)transform)
		{
			vector = transform.position;
			rotation = transform.rotation;
		}
		if (!component)
		{
			return;
		}
		Inventory inventory = component.inventory;
		if (!inventory)
		{
			return;
		}
		InteractionProcFilter interactionProcFilter = interactableObject.GetComponent<InteractionProcFilter>();
		int itemCount = inventory.GetItemCount(RoR2Content.Items.Firework);
		if (itemCount > 0 && InteractableIsPermittedForSpawn((MonoBehaviour)interactable))
		{
			Transform transform2 = interactableObject.GetComponent<ModelLocator>()?.modelTransform?.GetComponent<ChildLocator>()?.FindChild("FireworkOrigin");
			Vector3 position = (transform2 ? transform2.position : (interactableObject.transform.position + Vector3.up * 2f));
			int remaining = 4 + itemCount * 4;
			FireworkLauncher component2 = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/FireworkLauncher"), position, Quaternion.identity).GetComponent<FireworkLauncher>();
			component2.owner = interactor.gameObject;
			component2.crit = Util.CheckRoll(component.crit, component.master);
			component2.remaining = remaining;
		}
		int squidStacks = inventory.GetItemCount(RoR2Content.Items.Squid);
		if (squidStacks > 0 && InteractableIsPermittedForSpawn((MonoBehaviour)interactable))
		{
			CharacterSpawnCard spawnCard = LegacyResourcesAPI.Load<CharacterSpawnCard>("SpawnCards/CharacterSpawnCards/cscSquidTurret");
			DirectorPlacementRule placementRule = new DirectorPlacementRule
			{
				placementMode = DirectorPlacementRule.PlacementMode.Approximate,
				minDistance = 5f,
				maxDistance = 25f,
				position = interactableObject.transform.position
			};
			DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, RoR2Application.rng);
			directorSpawnRequest.teamIndexOverride = TeamIndex.Player;
			directorSpawnRequest.summonerBodyObject = interactor.gameObject;
			directorSpawnRequest.onSpawnedServer = (Action<SpawnCard.SpawnResult>)Delegate.Combine(directorSpawnRequest.onSpawnedServer, (Action<SpawnCard.SpawnResult>)delegate(SpawnCard.SpawnResult result)
			{
				if (result.success && (bool)result.spawnedInstance)
				{
					CharacterMaster component3 = result.spawnedInstance.GetComponent<CharacterMaster>();
					if ((bool)component3 && (bool)component3.inventory)
					{
						component3.inventory.GiveItem(RoR2Content.Items.HealthDecay, 30);
						component3.inventory.GiveItem(RoR2Content.Items.BoostAttackSpeed, 10 * (squidStacks - 1));
					}
				}
			});
			DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
		}
		int itemCount2 = inventory.GetItemCount(RoR2Content.Items.MonstersOnShrineUse);
		if (itemCount2 <= 0)
		{
			return;
		}
		PurchaseInteraction component4 = interactableObject.GetComponent<PurchaseInteraction>();
		if (!component4 || !component4.isShrine || (bool)interactableObject.GetComponent<ShrineCombatBehavior>())
		{
			return;
		}
		GameObject gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/Encounters/MonstersOnShrineUseEncounter");
		if (!gameObject)
		{
			return;
		}
		GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, vector, Quaternion.identity);
		NetworkServer.Spawn(gameObject2);
		CombatDirector component5 = gameObject2.GetComponent<CombatDirector>();
		if ((bool)component5 && (bool)Stage.instance)
		{
			float monsterCredit = 40f * Stage.instance.entryDifficultyCoefficient * (float)itemCount2;
			DirectorCard directorCard = component5.SelectMonsterCardForCombatShrine(monsterCredit);
			if (directorCard != null)
			{
				component5.CombatShrineActivation(interactor, monsterCredit, directorCard);
				EffectData effectData = new EffectData
				{
					origin = vector,
					rotation = rotation
				};
				EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MonstersOnShrineUse"), effectData, transmit: true);
			}
			else
			{
				NetworkServer.Destroy(gameObject2);
			}
		}
		bool InteractableIsPermittedForSpawn(MonoBehaviour interactableAsMonoBehaviour)
		{
			if (!interactableAsMonoBehaviour)
			{
				return false;
			}
			if ((bool)interactionProcFilter)
			{
				return interactionProcFilter.shouldAllowOnInteractionBeginProc;
			}
			if ((bool)interactableAsMonoBehaviour.GetComponent<DelusionChestController>())
			{
				if (interactableAsMonoBehaviour.GetComponent<PickupPickerController>().enabled)
				{
					return false;
				}
				return true;
			}
			if ((bool)interactableAsMonoBehaviour.GetComponent<GenericPickupController>())
			{
				return false;
			}
			if ((bool)interactableAsMonoBehaviour.GetComponent<VehicleSeat>())
			{
				return false;
			}
			if ((bool)interactableAsMonoBehaviour.GetComponent<NetworkUIPromptController>())
			{
				return false;
			}
			return true;
		}
	}

	public static void ClientDamageNotified(DamageDealtMessage damageDealtMessage)
	{
		GlobalEventManager.onClientDamageNotified?.Invoke(damageDealtMessage);
	}

	public static void ServerDamageDealt(DamageReport damageReport)
	{
		GlobalEventManager.onServerDamageDealt?.Invoke(damageReport);
	}

	public static void ServerCharacterExecuted(DamageReport damageReport, float executionHealthLost)
	{
		GlobalEventManager.onServerCharacterExecuted?.Invoke(damageReport, executionHealthLost);
	}
}

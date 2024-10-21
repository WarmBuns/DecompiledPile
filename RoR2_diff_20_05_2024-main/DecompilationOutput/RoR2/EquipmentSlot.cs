using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RoR2.DirectionalSearch;
using RoR2.Orbs;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
public class EquipmentSlot : NetworkBehaviour
{
	private struct UserTargetInfo
	{
		public readonly HurtBox hurtBox;

		public readonly GameObject rootObject;

		public readonly GenericPickupController pickupController;

		public readonly Transform transformToIndicateAt;

		public UserTargetInfo(HurtBox source)
		{
			hurtBox = source;
			rootObject = (hurtBox ? hurtBox.healthComponent.gameObject : null);
			pickupController = null;
			transformToIndicateAt = (hurtBox ? hurtBox.transform : null);
		}

		public UserTargetInfo(GenericPickupController source)
		{
			pickupController = source;
			hurtBox = null;
			rootObject = (pickupController ? pickupController.gameObject : null);
			transformToIndicateAt = (pickupController ? pickupController.pickupDisplay.transform : null);
		}
	}

	private Inventory inventory;

	private Run.FixedTimeStamp _rechargeTime;

	private bool hasEffectiveAuthority;

	private Xoroshiro128Plus rng;

	private HealthComponent healthComponent;

	private InputBankTest inputBank;

	private TeamComponent teamComponent;

	private const float fullCritDuration = 8f;

	private static readonly float tonicBuffDuration;

	public static string equipmentActivateString;

	private float missileTimer;

	private float bfgChargeTimer;

	private float subcooldownTimer;

	private const float missileInterval = 0.125f;

	private int remainingMissiles;

	private HealingFollowerController passiveHealingFollower;

	private GameObject goldgatControllerObject;

	private static int activeParamHash;

	private static int activeDurationParamHash;

	private static int activeStopwatchParamHash;

	private Indicator targetIndicator;

	private BullseyeSearch targetFinder = new BullseyeSearch();

	private UserTargetInfo currentTarget;

	private PickupSearch pickupSearch;

	private Transform sproutSpawnTransform;

	private GameObject playerRespawnEffectPrefab;

	private bool sproutIsSpawned;

	private GameObject sprout;

	private static int kRpcRpcOnClientEquipmentActivationRecieved;

	private static int kCmdCmdExecuteIfReady;

	private static int kCmdCmdOnEquipmentExecuted;

	public byte activeEquipmentSlot { get; private set; }

	public EquipmentIndex equipmentIndex { get; private set; }

	public int stock { get; private set; }

	public int maxStock { get; private set; }

	private bool equipmentDisabled
	{
		get
		{
			if (!inventory)
			{
				return false;
			}
			return inventory.GetEquipmentDisabled();
		}
	}

	public CharacterBody characterBody { get; private set; }

	public float cooldownTimer => _rechargeTime.timeUntil;

	public static event Action<EquipmentSlot, EquipmentIndex> onServerEquipmentActivated;

	public override void OnStartServer()
	{
		base.OnStartServer();
		UpdateAuthority();
	}

	public override void OnStartAuthority()
	{
		base.OnStartAuthority();
		UpdateAuthority();
	}

	public override void OnStopAuthority()
	{
		base.OnStopAuthority();
		UpdateAuthority();
	}

	private void UpdateAuthority()
	{
		hasEffectiveAuthority = Util.HasEffectiveAuthority(base.gameObject);
	}

	private void UpdateInventory()
	{
		inventory = characterBody.inventory;
		if ((bool)inventory)
		{
			activeEquipmentSlot = inventory.activeEquipmentSlot;
			equipmentIndex = inventory.GetEquipmentIndex();
			stock = inventory.GetEquipment(inventory.activeEquipmentSlot).charges;
			maxStock = inventory.GetActiveEquipmentMaxCharges();
			_rechargeTime = inventory.GetEquipment(inventory.activeEquipmentSlot).chargeFinishTime;
		}
		else
		{
			activeEquipmentSlot = 0;
			equipmentIndex = EquipmentIndex.None;
			stock = 0;
			maxStock = 0;
			_rechargeTime = Run.FixedTimeStamp.positiveInfinity;
		}
	}

	[Server]
	private void UpdateGoldGat()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.EquipmentSlot::UpdateGoldGat()' called on client");
			return;
		}
		bool flag = equipmentIndex == RoR2Content.Equipment.GoldGat.equipmentIndex;
		if (flag != (bool)goldgatControllerObject)
		{
			if (flag)
			{
				goldgatControllerObject = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/GoldGatController"));
				goldgatControllerObject.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(base.gameObject);
			}
			else
			{
				UnityEngine.Object.Destroy(goldgatControllerObject);
			}
		}
	}

	public Transform FindActiveEquipmentDisplay()
	{
		ModelLocator component = GetComponent<ModelLocator>();
		if ((bool)component)
		{
			Transform modelTransform = component.modelTransform;
			if ((bool)modelTransform)
			{
				CharacterModel component2 = modelTransform.GetComponent<CharacterModel>();
				if ((bool)component2)
				{
					List<GameObject> equipmentDisplayObjects = component2.GetEquipmentDisplayObjects(equipmentIndex);
					if (equipmentDisplayObjects.Count > 0)
					{
						return equipmentDisplayObjects[0].transform;
					}
				}
			}
		}
		return null;
	}

	[ClientRpc]
	private void RpcOnClientEquipmentActivationRecieved()
	{
		Util.PlaySound(equipmentActivateString, base.gameObject);
		EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
		if (equipmentDef == RoR2Content.Equipment.DroneBackup)
		{
			Util.PlaySound("Play_item_use_radio", base.gameObject);
		}
		else if (equipmentDef == RoR2Content.Equipment.BFG)
		{
			Transform transform = FindActiveEquipmentDisplay();
			if ((bool)transform)
			{
				Animator componentInChildren = transform.GetComponentInChildren<Animator>();
				if ((bool)componentInChildren)
				{
					componentInChildren.SetTrigger("Fire");
				}
			}
		}
		else if (equipmentDef == RoR2Content.Equipment.Blackhole)
		{
			Transform transform2 = FindActiveEquipmentDisplay();
			if ((bool)transform2)
			{
				GravCubeController component = transform2.GetComponent<GravCubeController>();
				if ((bool)component)
				{
					component.ActivateCube(9f);
				}
			}
		}
		else if (equipmentDef == RoR2Content.Equipment.CritOnUse)
		{
			Transform transform3 = FindActiveEquipmentDisplay();
			if ((bool)transform3)
			{
				Animator componentInChildren2 = transform3.GetComponentInChildren<Animator>();
				if ((bool)componentInChildren2)
				{
					componentInChildren2.SetBool(activeParamHash, value: true);
					componentInChildren2.SetFloat(activeDurationParamHash, 8f);
					componentInChildren2.SetFloat(activeStopwatchParamHash, 0f);
				}
			}
		}
		else if (equipmentDef == RoR2Content.Equipment.GainArmor)
		{
			Util.PlaySound("Play_item_use_gainArmor", base.gameObject);
		}
	}

	private void Start()
	{
		characterBody = GetComponent<CharacterBody>();
		healthComponent = GetComponent<HealthComponent>();
		inputBank = GetComponent<InputBankTest>();
		teamComponent = GetComponent<TeamComponent>();
		targetIndicator = new Indicator(base.gameObject, null);
		rng = new Xoroshiro128Plus(Run.instance.seed ^ (ulong)Run.instance.stageClearCount);
	}

	private void OnDestroy()
	{
		if (targetIndicator != null)
		{
			targetIndicator.active = false;
		}
	}

	private void FixedUpdate()
	{
		MyFixedUpdate(Time.fixedDeltaTime);
	}

	private void MyFixedUpdate(float deltaTime)
	{
		UpdateInventory();
		if (NetworkServer.active && !equipmentDisabled)
		{
			EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
			subcooldownTimer -= deltaTime;
			if (missileTimer > 0f)
			{
				missileTimer = Mathf.Max(missileTimer - deltaTime, 0f);
			}
			if (missileTimer == 0f && remainingMissiles > 0)
			{
				remainingMissiles--;
				missileTimer = 0.125f;
				FireMissile();
			}
			UpdateGoldGat();
			if (bfgChargeTimer > 0f)
			{
				bfgChargeTimer -= deltaTime;
				if (bfgChargeTimer < 0f)
				{
					_ = base.transform.position;
					Ray aimRay = GetAimRay();
					Transform transform = FindActiveEquipmentDisplay();
					if ((bool)transform)
					{
						ChildLocator componentInChildren = transform.GetComponentInChildren<ChildLocator>();
						if ((bool)componentInChildren)
						{
							Transform transform2 = componentInChildren.FindChild("Muzzle");
							if ((bool)transform2)
							{
								aimRay.origin = transform2.position;
							}
						}
					}
					healthComponent.TakeDamageForce(aimRay.direction * -1500f, alwaysApply: true);
					ProjectileManager.instance.FireProjectile(LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/BeamSphere"), aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, characterBody.damage * 2f, 0f, Util.CheckRoll(characterBody.crit, characterBody.master), DamageColorIndex.Item);
					bfgChargeTimer = 0f;
				}
			}
			if (equipmentDef == RoR2Content.Equipment.PassiveHealing != (bool)passiveHealingFollower)
			{
				if (!passiveHealingFollower)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/HealingFollower"), base.transform.position, Quaternion.identity);
					passiveHealingFollower = gameObject.GetComponent<HealingFollowerController>();
					passiveHealingFollower.NetworkownerBodyObject = base.gameObject;
					NetworkServer.Spawn(gameObject);
				}
				else
				{
					UnityEngine.Object.Destroy(passiveHealingFollower.gameObject);
					passiveHealingFollower = null;
				}
			}
		}
		bool num = inputBank.activateEquipment.justPressed || (inventory?.GetItemCount(RoR2Content.Items.AutoCastEquipment) ?? 0) > 0;
		bool isEquipmentActivationAllowed = characterBody.isEquipmentActivationAllowed;
		if (num && isEquipmentActivationAllowed && hasEffectiveAuthority)
		{
			if (NetworkServer.active)
			{
				ExecuteIfReady();
			}
			else
			{
				CallCmdExecuteIfReady();
			}
		}
	}

	[Command]
	private void CmdExecuteIfReady()
	{
		ExecuteIfReady();
	}

	[Server]
	public bool ExecuteIfReady()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.EquipmentSlot::ExecuteIfReady()' called on client");
			return false;
		}
		if (equipmentIndex != EquipmentIndex.None && stock > 0)
		{
			Execute();
			return true;
		}
		return false;
	}

	[Server]
	private void Execute()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.EquipmentSlot::Execute()' called on client");
			return;
		}
		EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
		if ((object)equipmentDef != null && subcooldownTimer <= 0f && PerformEquipmentAction(equipmentDef))
		{
			OnEquipmentExecuted();
		}
	}

	[Command]
	public void CmdOnEquipmentExecuted()
	{
		OnEquipmentExecuted();
	}

	[Server]
	public void OnEquipmentExecuted()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.EquipmentSlot::OnEquipmentExecuted()' called on client");
			return;
		}
		EquipmentIndex arg = this.equipmentIndex;
		inventory.DeductEquipmentCharges(activeEquipmentSlot, 1);
		UpdateInventory();
		CallRpcOnClientEquipmentActivationRecieved();
		EquipmentSlot.onServerEquipmentActivated?.Invoke(this, arg);
		if (!characterBody || !inventory)
		{
			return;
		}
		int itemCount = inventory.GetItemCount(RoR2Content.Items.EnergizedOnEquipmentUse);
		if (itemCount > 0)
		{
			characterBody.AddTimedBuff(RoR2Content.Buffs.Energized, 8 + 4 * (itemCount - 1));
		}
		int itemCount2 = inventory.GetItemCount(DLC1Content.Items.RandomEquipmentTrigger);
		if (itemCount2 <= 0 || EquipmentCatalog.randomTriggerEquipmentList.Count <= 0)
		{
			return;
		}
		List<EquipmentIndex> list = new List<EquipmentIndex>(EquipmentCatalog.randomTriggerEquipmentList);
		if (inventory.currentEquipmentIndex != EquipmentIndex.None)
		{
			list.Remove(inventory.currentEquipmentIndex);
		}
		Util.ShuffleList(list, rng);
		if (inventory.currentEquipmentIndex != EquipmentIndex.None)
		{
			list.Add(inventory.currentEquipmentIndex);
		}
		int num = 0;
		bool flag = false;
		bool flag2 = false;
		for (int i = 0; i < itemCount2; i++)
		{
			if (flag2)
			{
				break;
			}
			EquipmentIndex equipmentIndex = EquipmentIndex.None;
			do
			{
				if (num >= list.Count)
				{
					if (!flag)
					{
						flag2 = true;
						break;
					}
					flag = false;
					num %= list.Count;
				}
				equipmentIndex = list[num];
				num++;
			}
			while (!PerformEquipmentAction(EquipmentCatalog.GetEquipmentDef(equipmentIndex)));
			if (equipmentIndex == RoR2Content.Equipment.BFG.equipmentIndex)
			{
				ModelLocator component = GetComponent<ModelLocator>();
				if ((bool)component)
				{
					Transform modelTransform = component.modelTransform;
					if ((bool)modelTransform)
					{
						CharacterModel component2 = modelTransform.GetComponent<CharacterModel>();
						if ((bool)component2)
						{
							List<GameObject> itemDisplayObjects = component2.GetItemDisplayObjects(DLC1Content.Items.RandomEquipmentTrigger.itemIndex);
							if (itemDisplayObjects.Count > 0)
							{
								UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/BFG/ChargeBFG.prefab").WaitForCompletion(), itemDisplayObjects[0].transform);
							}
						}
					}
				}
			}
			flag = true;
		}
		EffectData effectData = new EffectData();
		effectData.origin = characterBody.corePosition;
		effectData.SetNetworkedObjectReference(base.gameObject);
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/RandomEquipmentTriggerProcEffect"), effectData, transmit: true);
	}

	private void FireMissile()
	{
		GameObject projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/MissileProjectile");
		float num = 3f;
		bool isCrit = Util.CheckRoll(characterBody.crit, characterBody.master);
		MissileUtils.FireMissile(characterBody.corePosition, characterBody, default(ProcChainMask), null, characterBody.damage * num, isCrit, projectilePrefab, DamageColorIndex.Item, addMissileProc: false);
	}

	[Server]
	private bool PerformEquipmentAction(EquipmentDef equipmentDef)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.EquipmentSlot::PerformEquipmentAction(RoR2.EquipmentDef)' called on client");
			return false;
		}
		if (equipmentDisabled)
		{
			return false;
		}
		Func<bool> func = null;
		if (equipmentDef == RoR2Content.Equipment.CommandMissile)
		{
			func = FireCommandMissile;
		}
		else if (equipmentDef == RoR2Content.Equipment.Fruit)
		{
			func = FireFruit;
		}
		else if (equipmentDef == RoR2Content.Equipment.DroneBackup)
		{
			func = FireDroneBackup;
		}
		else if (equipmentDef == RoR2Content.Equipment.Meteor)
		{
			func = FireMeteor;
		}
		else if (equipmentDef == RoR2Content.Equipment.Blackhole)
		{
			func = FireBlackhole;
		}
		else if (equipmentDef == RoR2Content.Equipment.Saw)
		{
			func = FireSaw;
		}
		else if (equipmentDef == JunkContent.Equipment.OrbitalLaser)
		{
			func = FireOrbitalLaser;
		}
		else if (equipmentDef == JunkContent.Equipment.GhostGun)
		{
			func = FireGhostGun;
		}
		else if (equipmentDef == RoR2Content.Equipment.CritOnUse)
		{
			func = FireCritOnUse;
		}
		else if (equipmentDef == RoR2Content.Equipment.BFG)
		{
			func = FireBfg;
		}
		else if (equipmentDef == RoR2Content.Equipment.Jetpack)
		{
			func = FireJetpack;
		}
		else if (equipmentDef == RoR2Content.Equipment.Lightning)
		{
			func = FireLightning;
		}
		else if (equipmentDef == RoR2Content.Equipment.PassiveHealing)
		{
			func = FirePassiveHealing;
		}
		else if (equipmentDef == RoR2Content.Equipment.BurnNearby)
		{
			func = FireBurnNearby;
		}
		else if (equipmentDef == JunkContent.Equipment.SoulCorruptor)
		{
			func = FireSoulCorruptor;
		}
		else if (equipmentDef == RoR2Content.Equipment.Scanner)
		{
			func = FireScanner;
		}
		else if (equipmentDef == RoR2Content.Equipment.CrippleWard)
		{
			func = FireCrippleWard;
		}
		else if (equipmentDef == RoR2Content.Equipment.Gateway)
		{
			func = FireGateway;
		}
		else if (equipmentDef == RoR2Content.Equipment.Tonic)
		{
			func = FireTonic;
		}
		else if (equipmentDef == RoR2Content.Equipment.Cleanse)
		{
			func = FireCleanse;
		}
		else if (equipmentDef == RoR2Content.Equipment.FireBallDash)
		{
			func = FireFireBallDash;
		}
		else if (equipmentDef == RoR2Content.Equipment.Recycle)
		{
			func = FireRecycle;
		}
		else if (equipmentDef == RoR2Content.Equipment.GainArmor)
		{
			func = FireGainArmor;
		}
		else if (equipmentDef == RoR2Content.Equipment.LifestealOnHit)
		{
			func = FireLifeStealOnHit;
		}
		else if (equipmentDef == RoR2Content.Equipment.TeamWarCry)
		{
			func = FireTeamWarCry;
		}
		else if (equipmentDef == RoR2Content.Equipment.DeathProjectile)
		{
			func = FireDeathProjectile;
		}
		else if (equipmentDef == DLC1Content.Equipment.Molotov)
		{
			func = FireMolotov;
		}
		else if (equipmentDef == DLC1Content.Equipment.VendingMachine)
		{
			func = FireVendingMachine;
		}
		else if (equipmentDef == DLC1Content.Equipment.BossHunter)
		{
			func = FireBossHunter;
		}
		else if (equipmentDef == DLC1Content.Equipment.BossHunterConsumed)
		{
			func = FireBossHunterConsumed;
		}
		else if (equipmentDef == DLC1Content.Equipment.GummyClone)
		{
			func = FireGummyClone;
		}
		else if (equipmentDef == DLC1Content.Equipment.LunarPortalOnUse)
		{
			func = FireLunarPortalOnUse;
		}
		else if (equipmentDef == DLC2Content.Equipment.HealAndRevive)
		{
			func = FireHealAndRevive;
		}
		else if (equipmentDef == DLC2Content.Equipment.HealAndReviveConsumed)
		{
			func = FireSproutOfLife;
		}
		return func?.Invoke() ?? false;
	}

	private Ray GetAimRay()
	{
		Ray result = default(Ray);
		result.direction = inputBank.aimDirection;
		result.origin = inputBank.aimOrigin;
		return result;
	}

	[CanBeNull]
	[Server]
	private CharacterMaster SummonMaster([NotNull] GameObject masterPrefab, Vector3 position, Quaternion rotation)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'RoR2.CharacterMaster RoR2.EquipmentSlot::SummonMaster(UnityEngine.GameObject,UnityEngine.Vector3,UnityEngine.Quaternion)' called on client");
			return null;
		}
		return new MasterSummon
		{
			masterPrefab = masterPrefab,
			position = position,
			rotation = rotation,
			summonerBodyObject = base.gameObject,
			ignoreTeamMemberLimit = false
		}.Perform();
	}

	private void ConfigureTargetFinderBase()
	{
		targetFinder.teamMaskFilter = TeamMask.allButNeutral;
		targetFinder.teamMaskFilter.RemoveTeam(teamComponent.teamIndex);
		targetFinder.sortMode = BullseyeSearch.SortMode.Angle;
		targetFinder.filterByLoS = true;
		float extraRaycastDistance;
		Ray ray = CameraRigController.ModifyAimRayIfApplicable(GetAimRay(), base.gameObject, out extraRaycastDistance);
		targetFinder.searchOrigin = ray.origin;
		targetFinder.searchDirection = ray.direction;
		targetFinder.maxAngleFilter = 10f;
		targetFinder.viewer = characterBody;
	}

	private void ConfigureTargetFinderForEnemies()
	{
		ConfigureTargetFinderBase();
		targetFinder.teamMaskFilter = TeamMask.GetUnprotectedTeams(teamComponent.teamIndex);
		targetFinder.RefreshCandidates();
		targetFinder.FilterOutGameObject(base.gameObject);
	}

	private void ConfigureTargetFinderForBossesWithRewards()
	{
		ConfigureTargetFinderBase();
		targetFinder.teamMaskFilter = TeamMask.GetUnprotectedTeams(teamComponent.teamIndex);
		targetFinder.RefreshCandidates();
		targetFinder.FilterOutGameObject(base.gameObject);
	}

	private void ConfigureTargetFinderForFriendlies()
	{
		ConfigureTargetFinderBase();
		targetFinder.teamMaskFilter = TeamMask.none;
		targetFinder.teamMaskFilter.AddTeam(teamComponent.teamIndex);
		targetFinder.RefreshCandidates();
		targetFinder.FilterOutGameObject(base.gameObject);
	}

	private void InvalidateCurrentTarget()
	{
		currentTarget = default(UserTargetInfo);
	}

	private void UpdateTargets(EquipmentIndex targetingEquipmentIndex, bool userShouldAnticipateTarget)
	{
		bool flag = targetingEquipmentIndex == DLC1Content.Equipment.BossHunter.equipmentIndex;
		bool flag2 = (targetingEquipmentIndex == RoR2Content.Equipment.Lightning.equipmentIndex || targetingEquipmentIndex == JunkContent.Equipment.SoulCorruptor.equipmentIndex || flag) && userShouldAnticipateTarget;
		bool flag3 = targetingEquipmentIndex == RoR2Content.Equipment.PassiveHealing.equipmentIndex && userShouldAnticipateTarget;
		bool num = flag2 || flag3;
		bool flag4 = targetingEquipmentIndex == RoR2Content.Equipment.Recycle.equipmentIndex;
		if (num)
		{
			if (flag2)
			{
				ConfigureTargetFinderForEnemies();
			}
			if (flag3)
			{
				ConfigureTargetFinderForFriendlies();
			}
			HurtBox source = null;
			if (flag)
			{
				foreach (HurtBox result in targetFinder.GetResults())
				{
					if ((bool)result && (bool)result.healthComponent && (bool)result.healthComponent.body)
					{
						DeathRewards component = result.healthComponent.body.gameObject.GetComponent<DeathRewards>();
						if ((bool)component && (bool)component.bossDropTable && !result.healthComponent.body.HasBuff(RoR2Content.Buffs.Immune))
						{
							source = result;
							break;
						}
					}
				}
			}
			else
			{
				source = targetFinder.GetResults().FirstOrDefault();
			}
			currentTarget = new UserTargetInfo(source);
		}
		else if (flag4)
		{
			currentTarget = new UserTargetInfo(FindPickupController(GetAimRay(), 10f, 30f, requireLoS: true, targetingEquipmentIndex == RoR2Content.Equipment.Recycle.equipmentIndex));
		}
		else
		{
			currentTarget = default(UserTargetInfo);
		}
		GenericPickupController pickupController = currentTarget.pickupController;
		bool flag5 = currentTarget.transformToIndicateAt;
		if (flag5)
		{
			if (targetingEquipmentIndex == RoR2Content.Equipment.Lightning.equipmentIndex)
			{
				targetIndicator.visualizerPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/LightningIndicator");
			}
			else if (targetingEquipmentIndex == RoR2Content.Equipment.PassiveHealing.equipmentIndex)
			{
				targetIndicator.visualizerPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/WoodSpriteIndicator");
			}
			else if (targetingEquipmentIndex == RoR2Content.Equipment.Recycle.equipmentIndex)
			{
				if (!pickupController.Recycled)
				{
					targetIndicator.visualizerPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/RecyclerIndicator");
				}
				else
				{
					targetIndicator.visualizerPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/RecyclerBadIndicator");
				}
			}
			else if (targetingEquipmentIndex == DLC1Content.Equipment.BossHunter.equipmentIndex)
			{
				targetIndicator.visualizerPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/BossHunterIndicator");
			}
			else
			{
				targetIndicator.visualizerPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/LightningIndicator");
			}
		}
		targetIndicator.active = flag5;
		targetIndicator.targetTransform = (flag5 ? currentTarget.transformToIndicateAt : null);
	}

	private GenericPickupController FindPickupController(Ray aimRay, float maxAngle, float maxDistance, bool requireLoS, bool requireTransmutable)
	{
		if (pickupSearch == null)
		{
			pickupSearch = new PickupSearch();
		}
		aimRay = CameraRigController.ModifyAimRayIfApplicable(aimRay, base.gameObject, out var extraRaycastDistance);
		pickupSearch.searchOrigin = aimRay.origin;
		pickupSearch.searchDirection = aimRay.direction;
		pickupSearch.minAngleFilter = 0f;
		pickupSearch.maxAngleFilter = maxAngle;
		pickupSearch.minDistanceFilter = 0f;
		pickupSearch.maxDistanceFilter = maxDistance + extraRaycastDistance;
		pickupSearch.filterByDistinctEntity = false;
		pickupSearch.filterByLoS = requireLoS;
		pickupSearch.sortMode = SortMode.DistanceAndAngle;
		pickupSearch.requireTransmutable = requireTransmutable;
		return pickupSearch.SearchCandidatesForSingleTarget(InstanceTracker.GetInstancesList<GenericPickupController>());
	}

	private void Update()
	{
		if (!equipmentDisabled)
		{
			UpdateTargets(equipmentIndex, stock > 0);
		}
	}

	private bool FireCommandMissile()
	{
		remainingMissiles += 12;
		return true;
	}

	private bool FireFruit()
	{
		if ((bool)healthComponent)
		{
			EffectData effectData = new EffectData();
			effectData.origin = base.transform.position;
			effectData.SetNetworkedObjectReference(base.gameObject);
			EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/FruitHealEffect"), effectData, transmit: true);
			healthComponent.HealFraction(0.5f, default(ProcChainMask));
		}
		return true;
	}

	private bool FireDroneBackup()
	{
		int sliceCount = 4;
		float num = 25f;
		if (NetworkServer.active)
		{
			float y = Quaternion.LookRotation(GetAimRay().direction).eulerAngles.y;
			float num2 = 3f;
			foreach (float item in new DegreeSlices(sliceCount, 0.5f))
			{
				Quaternion quaternion = Quaternion.Euler(-30f, y + item, 0f);
				Quaternion rotation = Quaternion.Euler(0f, y + item + 180f, 0f);
				Vector3 position = base.transform.position + quaternion * (Vector3.forward * num2);
				CharacterMaster characterMaster = SummonMaster(LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterMasters/DroneBackupMaster"), position, rotation);
				if ((bool)characterMaster)
				{
					characterMaster.gameObject.AddComponent<MasterSuicideOnTimer>().lifeTimer = num + UnityEngine.Random.Range(0f, 3f);
				}
			}
		}
		subcooldownTimer = 0.5f;
		return true;
	}

	private bool FireMeteor()
	{
		MeteorStormController component = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/MeteorStorm"), characterBody.corePosition, Quaternion.identity).GetComponent<MeteorStormController>();
		component.owner = base.gameObject;
		component.ownerDamage = characterBody.damage;
		component.isCrit = Util.CheckRoll(characterBody.crit, characterBody.master);
		NetworkServer.Spawn(component.gameObject);
		return true;
	}

	private bool FireBlackhole()
	{
		Vector3 position = base.transform.position;
		Ray aimRay = GetAimRay();
		ProjectileManager.instance.FireProjectile(LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/GravSphere"), position, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, 0f, 0f, crit: false);
		return true;
	}

	private bool FireSaw()
	{
		Ray aimRay = GetAimRay();
		Quaternion quaternion = Quaternion.LookRotation(aimRay.direction);
		float num = 15f;
		FireSingleSaw(characterBody, aimRay.origin, quaternion * Quaternion.Euler(0f, 0f - num, 0f));
		FireSingleSaw(characterBody, aimRay.origin, quaternion);
		FireSingleSaw(characterBody, aimRay.origin, quaternion * Quaternion.Euler(0f, num, 0f));
		return true;
		void FireSingleSaw(CharacterBody firingCharacterBody, Vector3 origin, Quaternion rotation)
		{
			GameObject projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/Sawmerang");
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.projectilePrefab = projectilePrefab;
			fireProjectileInfo.crit = characterBody.RollCrit();
			fireProjectileInfo.damage = characterBody.damage;
			fireProjectileInfo.damageColorIndex = DamageColorIndex.Item;
			fireProjectileInfo.force = 0f;
			fireProjectileInfo.owner = base.gameObject;
			fireProjectileInfo.position = origin;
			fireProjectileInfo.rotation = rotation;
			FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
			ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
		}
	}

	private bool FireOrbitalLaser()
	{
		Vector3 position = base.transform.position;
		if (Physics.Raycast(GetAimRay(), out var hitInfo, 900f, (int)LayerIndex.world.mask | (int)LayerIndex.CommonMasks.characterBodiesOrDefault))
		{
			position = hitInfo.point;
		}
		GameObject obj = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/OrbitalLaser"), position, Quaternion.identity);
		obj.GetComponent<OrbitalLaserController>().ownerBody = characterBody;
		NetworkServer.Spawn(obj);
		return true;
	}

	private bool FireGhostGun()
	{
		GameObject obj = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/GhostGun"), base.transform.position, Quaternion.identity);
		obj.GetComponent<GhostGunController>().owner = base.gameObject;
		NetworkServer.Spawn(obj);
		return true;
	}

	private bool FireCritOnUse()
	{
		characterBody.AddTimedBuff(RoR2Content.Buffs.FullCrit, 8f);
		return true;
	}

	private bool FireBfg()
	{
		bfgChargeTimer = 2f;
		subcooldownTimer = 2.2f;
		return true;
	}

	private bool FireJetpack()
	{
		JetpackController jetpackController = JetpackController.FindJetpackController(base.gameObject);
		if (!jetpackController)
		{
			UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/BodyAttachments/JetpackController")).GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(base.gameObject);
		}
		else
		{
			jetpackController.ResetTimer();
		}
		return true;
	}

	private bool FireLightning()
	{
		UpdateTargets(RoR2Content.Equipment.Lightning.equipmentIndex, userShouldAnticipateTarget: true);
		HurtBox hurtBox = currentTarget.hurtBox;
		if ((bool)hurtBox)
		{
			subcooldownTimer = 0.2f;
			OrbManager.instance.AddOrb(new LightningStrikeOrb
			{
				attacker = base.gameObject,
				damageColorIndex = DamageColorIndex.Item,
				damageValue = characterBody.damage * 30f,
				isCrit = Util.CheckRoll(characterBody.crit, characterBody.master),
				procChainMask = default(ProcChainMask),
				procCoefficient = 1f,
				target = hurtBox
			});
			InvalidateCurrentTarget();
			return true;
		}
		return false;
	}

	private bool FireBossHunter()
	{
		UpdateTargets(DLC1Content.Equipment.BossHunter.equipmentIndex, userShouldAnticipateTarget: true);
		HurtBox hurtBox = currentTarget.hurtBox;
		DeathRewards deathRewards = hurtBox?.healthComponent?.body?.gameObject?.GetComponent<DeathRewards>();
		if ((bool)hurtBox && (bool)deathRewards)
		{
			Vector3 vector = (hurtBox.transform ? hurtBox.transform.position : Vector3.zero);
			Vector3 normalized = (vector - characterBody.corePosition).normalized;
			PickupDropletController.CreatePickupDroplet(deathRewards.bossDropTable.GenerateDrop(rng), vector, normalized * 15f);
			if ((bool)hurtBox?.healthComponent?.body?.master)
			{
				hurtBox.healthComponent.body.master.TrueKill(base.gameObject);
			}
			CharacterModel component = hurtBox.hurtBoxGroup.GetComponent<CharacterModel>();
			if ((bool)component)
			{
				TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(component.gameObject);
				temporaryOverlayInstance.duration = 0.1f;
				temporaryOverlayInstance.animateShaderAlpha = true;
				temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlayInstance.destroyComponentOnEnd = true;
				temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashBright");
				temporaryOverlayInstance.AddToCharacterModel(component);
				TemporaryOverlayInstance temporaryOverlayInstance2 = TemporaryOverlayManager.AddOverlay(component.gameObject);
				temporaryOverlayInstance2.duration = 1.2f;
				temporaryOverlayInstance2.animateShaderAlpha = true;
				temporaryOverlayInstance2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlayInstance2.destroyComponentOnEnd = true;
				temporaryOverlayInstance2.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matGhostEffect");
				temporaryOverlayInstance2.AddToCharacterModel(component);
			}
			DamageInfo damageInfo = new DamageInfo();
			damageInfo.attacker = base.gameObject;
			damageInfo.force = -normalized * 2500f;
			healthComponent.TakeDamageForce(damageInfo, alwaysApply: true);
			GameObject effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/BossHunterKillEffect");
			Quaternion rotation = Util.QuaternionSafeLookRotation(normalized, Vector3.up);
			EffectManager.SpawnEffect(effectPrefab, new EffectData
			{
				origin = vector,
				rotation = rotation
			}, transmit: true);
			CharacterModel characterModel = base.gameObject.GetComponent<ModelLocator>()?.modelTransform?.GetComponent<CharacterModel>();
			if ((bool)characterModel)
			{
				foreach (GameObject equipmentDisplayObject in characterModel.GetEquipmentDisplayObjects(DLC1Content.Equipment.BossHunter.equipmentIndex))
				{
					if (equipmentDisplayObject.name.Contains("DisplayTricorn"))
					{
						EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/BossHunterHatEffect"), new EffectData
						{
							origin = equipmentDisplayObject.transform.position,
							rotation = equipmentDisplayObject.transform.rotation,
							scale = equipmentDisplayObject.transform.localScale.x
						}, transmit: true);
					}
					else
					{
						EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/BossHunterGunEffect"), new EffectData
						{
							origin = equipmentDisplayObject.transform.position,
							rotation = Util.QuaternionSafeLookRotation(vector - equipmentDisplayObject.transform.position, Vector3.up),
							scale = equipmentDisplayObject.transform.localScale.x
						}, transmit: true);
					}
				}
			}
			if ((bool)characterBody?.inventory)
			{
				CharacterMasterNotificationQueue.SendTransformNotification(characterBody.master, characterBody.inventory.currentEquipmentIndex, DLC1Content.Equipment.BossHunterConsumed.equipmentIndex, CharacterMasterNotificationQueue.TransformationType.Default);
				characterBody.inventory.SetEquipmentIndex(DLC1Content.Equipment.BossHunterConsumed.equipmentIndex);
			}
			InvalidateCurrentTarget();
			return true;
		}
		return false;
	}

	private bool FireBossHunterConsumed()
	{
		if ((bool)characterBody)
		{
			Chat.SendBroadcastChat(new Chat.BodyChatMessage
			{
				bodyObject = characterBody.gameObject,
				token = "EQUIPMENT_BOSSHUNTERCONSUMED_CHAT"
			});
			subcooldownTimer = 1f;
		}
		return true;
	}

	private bool FirePassiveHealing()
	{
		UpdateTargets(RoR2Content.Equipment.PassiveHealing.equipmentIndex, userShouldAnticipateTarget: true);
		CharacterBody characterBody = currentTarget.rootObject?.GetComponent<CharacterBody>() ?? this.characterBody;
		if ((bool)characterBody)
		{
			EffectManager.SimpleImpactEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/WoodSpriteHeal"), characterBody.corePosition, Vector3.up, transmit: true);
			characterBody.healthComponent?.HealFraction(0.1f, default(ProcChainMask));
		}
		if ((bool)passiveHealingFollower)
		{
			passiveHealingFollower.AssignNewTarget(currentTarget.rootObject);
			InvalidateCurrentTarget();
		}
		return true;
	}

	private bool FireBurnNearby()
	{
		if ((bool)characterBody)
		{
			characterBody.AddHelfireDuration(12f);
		}
		return true;
	}

	private bool FireSoulCorruptor()
	{
		UpdateTargets(JunkContent.Equipment.SoulCorruptor.equipmentIndex, userShouldAnticipateTarget: true);
		HurtBox hurtBox = currentTarget.hurtBox;
		if (!hurtBox)
		{
			return false;
		}
		if (!hurtBox.healthComponent || hurtBox.healthComponent.combinedHealthFraction > 0.25f)
		{
			return false;
		}
		Util.TryToCreateGhost(hurtBox.healthComponent.body, characterBody, 30);
		hurtBox.healthComponent.Suicide(base.gameObject);
		InvalidateCurrentTarget();
		return true;
	}

	private bool FireScanner()
	{
		NetworkServer.Spawn(UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/ChestScanner"), characterBody.corePosition, Quaternion.identity));
		return true;
	}

	private bool FireCrippleWard()
	{
		characterBody.master.GetDeployableCount(DeployableSlot.PowerWard);
		Ray aimRay = GetAimRay();
		float maxDistance = 1000f;
		if (Physics.Raycast(aimRay, out var hitInfo, maxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
		{
			GameObject obj = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/CrippleWard"), hitInfo.point, Util.QuaternionSafeLookRotation(hitInfo.normal, Vector3.forward));
			Deployable component = obj.GetComponent<Deployable>();
			characterBody.master.AddDeployable(component, DeployableSlot.CrippleWard);
			NetworkServer.Spawn(obj);
			return true;
		}
		return true;
	}

	private bool FireTonic()
	{
		characterBody.AddTimedBuff(RoR2Content.Buffs.TonicBuff, tonicBuffDuration);
		if (!Util.CheckRoll(80f, characterBody.master))
		{
			characterBody.pendingTonicAfflictionCount++;
		}
		return true;
	}

	private bool FireCleanse()
	{
		Vector3 corePosition = characterBody.corePosition;
		EffectData effectData = new EffectData
		{
			origin = corePosition
		};
		effectData.SetHurtBoxReference(characterBody.mainHurtBox);
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/CleanseEffect"), effectData, transmit: true);
		Util.CleanseBody(characterBody, removeDebuffs: true, removeBuffs: false, removeCooldownBuffs: true, removeDots: true, removeStun: true, removeNearbyProjectiles: true);
		return true;
	}

	private bool FireFireBallDash()
	{
		Ray aimRay = GetAimRay();
		GameObject gameObject = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/FireballVehicle"), aimRay.origin, Quaternion.LookRotation(aimRay.direction));
		gameObject.GetComponent<VehicleSeat>().AssignPassenger(base.gameObject);
		NetworkUser networkUser = characterBody?.master?.playerCharacterMasterController?.networkUser;
		if ((bool)networkUser)
		{
			NetworkServer.SpawnWithClientAuthority(gameObject, networkUser.gameObject);
		}
		else
		{
			NetworkServer.Spawn(gameObject);
		}
		subcooldownTimer = 2f;
		return true;
	}

	private bool FireGainArmor()
	{
		characterBody.AddTimedBuff(RoR2Content.Buffs.ElephantArmorBoost, 5f);
		return true;
	}

	private bool FireRecycle()
	{
		UpdateTargets(RoR2Content.Equipment.Recycle.equipmentIndex, userShouldAnticipateTarget: false);
		GenericPickupController pickupController = currentTarget.pickupController;
		if ((bool)pickupController && !pickupController.Recycled)
		{
			PickupIndex initialPickupIndex = pickupController.pickupIndex;
			subcooldownTimer = 0.2f;
			PickupIndex[] array = (from pickupIndex in PickupTransmutationManager.GetAvailableGroupFromPickupIndex(pickupController.pickupIndex)
				where pickupIndex != initialPickupIndex
				select pickupIndex).ToArray();
			if (array == null)
			{
				return false;
			}
			if (array.Length == 0)
			{
				return false;
			}
			pickupController.NetworkpickupIndex = Run.instance.treasureRng.NextElementUniform(array);
			EffectManager.SimpleEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OmniEffect/OmniRecycleEffect"), pickupController.pickupDisplay.transform.position, Quaternion.identity, transmit: true);
			pickupController.NetworkRecycled = true;
			InvalidateCurrentTarget();
			return true;
		}
		return false;
	}

	private bool FireGateway()
	{
		_ = characterBody.footPosition;
		Ray aimRay = GetAimRay();
		float num = 2f;
		float num2 = num * 2f;
		float maxDistance = 1000f;
		Rigidbody component = GetComponent<Rigidbody>();
		if (!component)
		{
			return false;
		}
		Vector3 position = base.transform.position;
		if (Physics.Raycast(aimRay, out var hitInfo, maxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
		{
			Vector3 vector = hitInfo.point + hitInfo.normal * num;
			Vector3 vector2 = vector - position;
			Vector3 normalized = vector2.normalized;
			Vector3 pointBPosition = vector;
			if (component.SweepTest(normalized, out var hitInfo2, vector2.magnitude))
			{
				if (hitInfo2.distance < num2)
				{
					return false;
				}
				pointBPosition = position + normalized * hitInfo2.distance;
			}
			GameObject obj = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/Zipline"));
			ZiplineController component2 = obj.GetComponent<ZiplineController>();
			component2.SetPointAPosition(position + normalized * num);
			component2.SetPointBPosition(pointBPosition);
			obj.AddComponent<DestroyOnTimer>().duration = 30f;
			NetworkServer.Spawn(obj);
			return true;
		}
		return false;
	}

	private bool FireLifeStealOnHit()
	{
		EffectData effectData = new EffectData
		{
			origin = characterBody.corePosition
		};
		effectData.SetHurtBoxReference(characterBody.gameObject);
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/LifeStealOnHitActivation"), effectData, transmit: false);
		characterBody.AddTimedBuff(RoR2Content.Buffs.LifeSteal, 8f);
		return true;
	}

	private bool FireTeamWarCry()
	{
		Util.PlaySound("Play_teamWarCry_activate", characterBody.gameObject);
		Vector3 corePosition = characterBody.corePosition;
		EffectData effectData = new EffectData
		{
			origin = corePosition
		};
		effectData.SetNetworkedObjectReference(characterBody.gameObject);
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/TeamWarCryActivation"), effectData, transmit: true);
		characterBody.AddTimedBuff(RoR2Content.Buffs.TeamWarCry, 7f);
		TeamComponent[] array = UnityEngine.Object.FindObjectsOfType<TeamComponent>();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].teamIndex == teamComponent.teamIndex)
			{
				array[i].GetComponent<CharacterBody>().AddTimedBuff(RoR2Content.Buffs.TeamWarCry, 7f);
			}
		}
		return true;
	}

	private bool FireDeathProjectile()
	{
		CharacterMaster master = characterBody.master;
		if (!master)
		{
			return false;
		}
		if (master.IsDeployableLimited(DeployableSlot.DeathProjectile))
		{
			return false;
		}
		Ray aimRay = GetAimRay();
		Quaternion rotation = Quaternion.LookRotation(aimRay.direction);
		GameObject gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/DeathProjectile");
		gameObject.GetComponent<DeathProjectile>().teamIndex = teamComponent.teamIndex;
		FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
		fireProjectileInfo.projectilePrefab = gameObject;
		fireProjectileInfo.crit = characterBody.RollCrit();
		fireProjectileInfo.damage = characterBody.damage;
		fireProjectileInfo.damageColorIndex = DamageColorIndex.Item;
		fireProjectileInfo.force = 0f;
		fireProjectileInfo.owner = base.gameObject;
		fireProjectileInfo.position = aimRay.origin;
		fireProjectileInfo.rotation = rotation;
		FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
		ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
		return true;
	}

	private bool FireMolotov()
	{
		Ray aimRay = GetAimRay();
		GameObject prefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/MolotovClusterProjectile");
		ProjectileManager.instance.FireProjectile(prefab, aimRay.origin, Quaternion.LookRotation(aimRay.direction), base.gameObject, characterBody.damage, 0f, Util.CheckRoll(characterBody.crit, characterBody.master));
		return true;
	}

	private bool FireVendingMachine()
	{
		Ray ray = new Ray(GetAimRay().origin, Vector3.down);
		if (Util.CharacterRaycast(base.gameObject, ray, out var hitInfo, 1000f, LayerIndex.world.mask, QueryTriggerInteraction.UseGlobal))
		{
			GameObject prefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/VendingMachineProjectile");
			ProjectileManager.instance.FireProjectile(prefab, hitInfo.point, Quaternion.identity, base.gameObject, characterBody.damage, 0f, Util.CheckRoll(characterBody.crit, characterBody.master));
			subcooldownTimer = 0.5f;
			return true;
		}
		return false;
	}

	private bool FireGummyClone()
	{
		CharacterMaster characterMaster = characterBody?.master;
		if (!characterMaster || characterMaster.IsDeployableLimited(DeployableSlot.GummyClone))
		{
			return false;
		}
		Ray aimRay = GetAimRay();
		Quaternion rotation = Quaternion.LookRotation(aimRay.direction);
		GameObject projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/GummyCloneProjectile");
		FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
		fireProjectileInfo.projectilePrefab = projectilePrefab;
		fireProjectileInfo.crit = characterBody.RollCrit();
		fireProjectileInfo.damage = 0f;
		fireProjectileInfo.damageColorIndex = DamageColorIndex.Item;
		fireProjectileInfo.force = 0f;
		fireProjectileInfo.owner = base.gameObject;
		fireProjectileInfo.position = aimRay.origin;
		fireProjectileInfo.rotation = rotation;
		FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
		ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
		return true;
	}

	private bool FireLunarPortalOnUse()
	{
		TeleporterInteraction.instance.shouldAttemptToSpawnShopPortal = true;
		return true;
	}

	private bool FireHealAndRevive()
	{
		bool flag = false;
		foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
		{
			CharacterMaster master = instance.master;
			if (!instance.isConnected || !master.IsDeadAndOutOfLivesServer())
			{
				continue;
			}
			flag = true;
			Vector3 deathFootPosition = master.deathFootPosition;
			master.Respawn(deathFootPosition, Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f));
			CharacterBody body = master.GetBody();
			if ((bool)body)
			{
				body.AddTimedBuff(RoR2Content.Buffs.Immune, 3f);
				EntityStateMachine[] components = body.GetComponents<EntityStateMachine>();
				foreach (EntityStateMachine obj in components)
				{
					obj.initialStateType = obj.mainStateType;
				}
				playerRespawnEffectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/fxHealAndReviveGold");
				if ((bool)playerRespawnEffectPrefab)
				{
					EffectManager.SpawnEffect(playerRespawnEffectPrefab, new EffectData
					{
						origin = deathFootPosition,
						rotation = body.transform.rotation
					}, transmit: true);
					Util.PlaySound("Play_item_use_healAndRevive_activate", base.gameObject);
				}
			}
		}
		if (flag)
		{
			bool flag2 = false;
			if (characterBody?.equipmentSlot != null && characterBody?.equipmentSlot.equipmentIndex == DLC2Content.Equipment.HealAndRevive.equipmentIndex)
			{
				flag2 = true;
			}
			if ((bool)characterBody?.inventory && flag2)
			{
				CharacterMasterNotificationQueue.SendTransformNotification(characterBody.master, characterBody.inventory.currentEquipmentIndex, DLC2Content.Equipment.HealAndReviveConsumed.equipmentIndex, CharacterMasterNotificationQueue.TransformationType.Default);
				characterBody.inventory.SetEquipmentIndex(DLC2Content.Equipment.HealAndReviveConsumed.equipmentIndex);
			}
		}
		if (!flag)
		{
			return false;
		}
		return true;
	}

	private bool FireSproutOfLife()
	{
		if (sproutIsSpawned && (bool)sprout)
		{
			sproutIsSpawned = false;
			NetworkServer.Destroy(sprout);
		}
		Ray aimRay = GetAimRay();
		Quaternion.LookRotation(aimRay.direction);
		sproutSpawnTransform = characterBody.transform;
		Vector3 position = characterBody.transform.position;
		characterBody.transform.TransformDirection(Vector3.forward);
		new Quaternion(0f, UnityEngine.Random.Range(0f, 360f), 0f, 1f);
		if (Physics.Raycast(position, aimRay.direction, out var hitInfo, float.PositiveInfinity, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
		{
			sproutSpawnTransform.position = hitInfo.point;
			sproutSpawnTransform.up = hitInfo.normal;
			sprout = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/SproutOfLifeHealing"), sproutSpawnTransform.position, Quaternion.identity);
			sprout.transform.up = sproutSpawnTransform.up;
			sprout.transform.rotation *= Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), Vector3.up);
			NetworkServer.Spawn(sprout);
			sproutIsSpawned = true;
			return true;
		}
		return false;
	}

	static EquipmentSlot()
	{
		tonicBuffDuration = 20f;
		equipmentActivateString = "Play_UI_equipment_activate";
		activeParamHash = Animator.StringToHash("active");
		activeDurationParamHash = Animator.StringToHash("activeDuration");
		activeStopwatchParamHash = Animator.StringToHash("activeStopwatch");
		kCmdCmdExecuteIfReady = -303452611;
		NetworkBehaviour.RegisterCommandDelegate(typeof(EquipmentSlot), kCmdCmdExecuteIfReady, InvokeCmdCmdExecuteIfReady);
		kCmdCmdOnEquipmentExecuted = 1725820338;
		NetworkBehaviour.RegisterCommandDelegate(typeof(EquipmentSlot), kCmdCmdOnEquipmentExecuted, InvokeCmdCmdOnEquipmentExecuted);
		kRpcRpcOnClientEquipmentActivationRecieved = 1342577121;
		NetworkBehaviour.RegisterRpcDelegate(typeof(EquipmentSlot), kRpcRpcOnClientEquipmentActivationRecieved, InvokeRpcRpcOnClientEquipmentActivationRecieved);
		NetworkCRC.RegisterBehaviour("EquipmentSlot", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdExecuteIfReady(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdExecuteIfReady called on client.");
		}
		else
		{
			((EquipmentSlot)obj).CmdExecuteIfReady();
		}
	}

	protected static void InvokeCmdCmdOnEquipmentExecuted(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdOnEquipmentExecuted called on client.");
		}
		else
		{
			((EquipmentSlot)obj).CmdOnEquipmentExecuted();
		}
	}

	public void CallCmdExecuteIfReady()
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdExecuteIfReady called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdExecuteIfReady();
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdExecuteIfReady);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendCommandInternal(networkWriter, 0, "CmdExecuteIfReady");
	}

	public void CallCmdOnEquipmentExecuted()
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdOnEquipmentExecuted called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdOnEquipmentExecuted();
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdOnEquipmentExecuted);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendCommandInternal(networkWriter, 0, "CmdOnEquipmentExecuted");
	}

	protected static void InvokeRpcRpcOnClientEquipmentActivationRecieved(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcOnClientEquipmentActivationRecieved called on server.");
		}
		else
		{
			((EquipmentSlot)obj).RpcOnClientEquipmentActivationRecieved();
		}
	}

	public void CallRpcOnClientEquipmentActivationRecieved()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcOnClientEquipmentActivationRecieved called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcOnClientEquipmentActivationRecieved);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcOnClientEquipmentActivationRecieved");
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}

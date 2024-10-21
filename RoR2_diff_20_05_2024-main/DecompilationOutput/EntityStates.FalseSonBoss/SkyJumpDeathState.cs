using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.FalseSonBoss;

public class SkyJumpDeathState : GenericCharacterDeath
{
	[SerializeField]
	public float duration = 4.5f;

	public GameObject deathEffectPrefab;

	public float vfxSize = 8f;

	private float beginUpwardMovementTime = 0.96086955f;

	private bool startedFlying;

	private Vector3 cachedDeathPosition;

	private bool _shouldAutoDestroy;

	[SerializeField]
	public GameObject falseSonJumpAwayVFX;

	[SerializeField]
	public ItemTier rewardDisplayTier;

	[SerializeField]
	[Tooltip("The prefab to use for the reward pickup.")]
	public GameObject rewardPickupPrefab;

	[SerializeField]
	public Vector3 rewardOffset;

	[SerializeField]
	public string falseSonSurvivorAchievement;

	private PickupIndex halcyonSeed;

	[SerializeField]
	public int rewardOptionCount = 1;

	[SerializeField]
	public PickupDropTable dtColossusBuffDropTable;

	public bool rewardGiven;

	private bool playedSFX;

	protected override bool shouldAutoDestroy => _shouldAutoDestroy;

	public static event Action falseSonDeathEvent;

	public static event Action falseSonUnlockEvent;

	public override void OnEnter()
	{
		base.OnEnter();
		SkyJumpDeathState.falseSonDeathEvent?.Invoke();
		beginUpwardMovementTime *= duration;
		cachedDeathPosition = base.gameObject.transform.position;
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("FalseSonBoss/FalseSonBossLightningStreakDeath"), new EffectData
		{
			origin = base.characterBody.corePosition,
			scale = vfxSize
		}, transmit: true);
		halcyonSeed = PickupCatalog.FindPickupIndex("TitanGoldDuringTP");
		GiveColossusItem();
	}

	protected override void PlayDeathAnimation(float crossfadeDuration = 0.1f)
	{
		PlayAnimation("FullBody, Override", "Phase3Death", "StepBrothersPrep.playbackRate", duration);
		Util.PlaySound("Play_boss_falseson_VO_defeat", base.gameObject);
		MeridianEventTriggerInteraction.instance.musicPhaseThree.SetActive(value: false);
		MeridianEventTriggerInteraction.instance.musicPhaseBossDead.SetActive(value: true);
	}

	public override void FixedUpdate()
	{
		if (base.fixedAge >= 0.35f && !playedSFX)
		{
			Util.PlaySound("Play_boss_falseson_phaseTransition_kneel", base.characterBody.gameObject);
			Util.PlaySound("Play_boss_falseson_VO_groan", base.characterBody.gameObject);
			playedSFX = true;
		}
		if (base.fixedAge > beginUpwardMovementTime)
		{
			if (!startedFlying)
			{
				if (NetworkServer.active)
				{
					LightningStormController.SetStormActive(b: false);
				}
				startedFlying = true;
				EffectManager.SpawnEffect(falseSonJumpAwayVFX, new EffectData
				{
					origin = cachedDeathPosition
				}, transmit: false);
			}
			base.characterMotor.rootMotion += Vector3.up * moveSpeedStat;
		}
		if (!_shouldAutoDestroy && NetworkServer.active && base.fixedAge >= duration + 0.5f)
		{
			_shouldAutoDestroy = true;
		}
		base.FixedUpdate();
	}

	public override void OnExit()
	{
		DestroyBodyAsapServer();
		DestroyModel();
		base.OnExit();
		MeridianEventTriggerInteraction.FSBFPhaseBaseState.readyToSpawnNextBossBody = true;
		DestroyBodyServer();
	}

	private void DestroyBodyServer()
	{
		if (NetworkServer.active)
		{
			OnPreDestroyBodyServer();
			EntityState.Destroy(base.gameObject);
		}
	}

	private void GiveColossusItem()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		int participatingPlayerCount = Run.instance.participatingPlayerCount;
		if (dtColossusBuffDropTable == null || participatingPlayerCount == 0)
		{
			return;
		}
		if (FalseSonUnlockStateMet())
		{
			SkyJumpDeathState.falseSonUnlockEvent?.Invoke();
			dtColossusBuffDropTable = LegacyResourcesAPI.Load<PickupDropTable>("DropTables/dtAurelioniteHeartPickupDropTable");
		}
		Xoroshiro128Plus rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
		if (participatingPlayerCount > 0 && (bool)base.gameObject && (bool)dtColossusBuffDropTable && !rewardGiven)
		{
			rewardGiven = true;
			int num = participatingPlayerCount;
			float angle = 360f / (float)num;
			Vector3 vector = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
			Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
			Vector3 position = cachedDeathPosition + rewardOffset;
			int num2 = 0;
			while (num2 < num)
			{
				GenericPickupController.CreatePickupInfo pickupInfo = default(GenericPickupController.CreatePickupInfo);
				pickupInfo.pickupIndex = PickupCatalog.FindPickupIndex(rewardDisplayTier);
				pickupInfo.pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(rewardOptionCount, dtColossusBuffDropTable, rng);
				pickupInfo.rotation = Quaternion.identity;
				pickupInfo.prefabOverride = rewardPickupPrefab;
				PickupDropletController.CreatePickupDroplet(pickupInfo, position, vector);
				num2++;
				vector = quaternion * vector;
			}
		}
	}

	private bool FalseSonUnlockStateMet()
	{
		bool result = false;
		if (MeridianEventTriggerInteraction.instance.isGoldTitanSpawned)
		{
			return true;
		}
		foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
		{
			if (!(instance == null) && instance.master.inventory.GetItemCount(RoR2Content.Items.TitanGoldDuringTP) > 0)
			{
				result = true;
				GameObject effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/DelusionItemDissolveVFX");
				EffectData effectData = new EffectData
				{
					origin = instance.master.GetBody().transform.position,
					genericFloat = 1.5f,
					genericUInt = (uint)(halcyonSeed.itemIndex + 1)
				};
				effectData.SetNetworkedObjectReference(base.gameObject);
				EffectManager.SpawnEffect(effectPrefab, effectData, transmit: true);
				instance.master.inventory.RemoveItem(halcyonSeed.itemIndex);
			}
		}
		return result;
	}
}

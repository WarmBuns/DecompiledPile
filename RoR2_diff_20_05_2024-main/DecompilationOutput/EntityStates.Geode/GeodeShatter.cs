using System.Collections.Generic;
using HG;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Geode;

public class GeodeShatter : GeodeEntityStates
{
	private SphereSearch sphereSearch;

	private List<HurtBox> hitHurtBoxes;

	private bool cleansingActive = true;

	private float stopWatch;

	protected bool rewardGiven;

	[SerializeField]
	[Tooltip("The prefab to use for the reward pickup.")]
	public GameObject rewardPickupPrefab;

	[SerializeField]
	public PickupDropTable rewardDropTable;

	[SerializeField]
	public int rewardOptionCount = 1;

	[SerializeField]
	public ItemTier rewardDisplayTier;

	[SerializeField]
	public Vector3 rewardOffset;

	[SerializeField]
	public float cleansingDuration = 6f;

	[SerializeField]
	public float cleansingTicksPerSecond = 1.5f;

	protected Xoroshiro128Plus rng;

	public override void OnEnter()
	{
		base.OnEnter();
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/GeodeShatterVFX"), new EffectData
		{
			origin = base.gameObject.transform.position
		}, transmit: false);
		Util.PlaySound("Play_env_meridian_geode_activate", base.gameObject);
		Util.PlaySound("Play_env_meridian_geode_active_loop", base.gameObject);
		cleansingIndicator.SetActive(value: true);
		sphereSearch = new SphereSearch();
		CharacterBody characterBody = geodeController.lastInteractor?.GetComponent<CharacterBody>();
		if (!geodeController.ShouldRegenerate)
		{
			geodeBase.SetActive(value: false);
		}
		geodeTrunkRenderer.material.SetFloat(sliceHeightNameId, geodeController.startingPrintHeight);
		geodeIdleVFX.SetActive(value: false);
		geodeModel.SetActive(value: false);
		if (!NetworkServer.active)
		{
			return;
		}
		if ((bool)characterBody && characterBody.HasBuff(DLC2Content.Buffs.GeodeBuff) && geodeController.ShouldDropReward)
		{
			rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
			RemoveGeodeBuffFromAllPlayers();
			int participatingPlayerCount = Run.instance.participatingPlayerCount;
			if (!(base.gameObject == null) && !(rewardDropTable == null) && participatingPlayerCount != 0 && participatingPlayerCount > 0 && (bool)base.gameObject && (bool)rewardDropTable && !rewardGiven)
			{
				rewardGiven = true;
				int num = participatingPlayerCount;
				float angle = 360f / (float)num;
				Vector3 vector = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
				Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
				Vector3 position = base.gameObject.transform.position + rewardOffset;
				int num2 = 0;
				while (num2 < num)
				{
					GenericPickupController.CreatePickupInfo pickupInfo = default(GenericPickupController.CreatePickupInfo);
					pickupInfo.pickupIndex = PickupCatalog.FindPickupIndex(rewardDisplayTier);
					pickupInfo.pickerOptions = PickupPickerController.GenerateOptionsFromDropTable(rewardOptionCount, rewardDropTable, rng);
					pickupInfo.rotation = Quaternion.identity;
					pickupInfo.prefabOverride = rewardPickupPrefab;
					PickupDropletController.CreatePickupDroplet(pickupInfo, position, vector);
					num2++;
					vector = quaternion * vector;
				}
			}
		}
		else
		{
			RemoveGeodeBuffFromAllPlayers();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= cleansingDuration)
		{
			cleansingActive = false;
			Util.PlaySound("Stop_env_meridian_geode_active_loop", base.gameObject);
			outer.SetNextState(new GeodeInert());
		}
		else
		{
			if (!cleansingActive)
			{
				return;
			}
			stopWatch += Time.deltaTime;
			if (stopWatch > 1f / cleansingTicksPerSecond && sphereSearch != null)
			{
				stopWatch -= 1f / cleansingTicksPerSecond;
				if (NetworkServer.active)
				{
					HandleCleanseNearbyPlayers();
				}
			}
		}
	}

	private void HandleCleanseNearbyPlayers()
	{
		hitHurtBoxes = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
		SearchForPlayers(hitHurtBoxes);
		for (int i = 0; i < hitHurtBoxes.Count; i++)
		{
			HurtBox hurtBox = hitHurtBoxes[i];
			CharacterBody body = hurtBox.healthComponent.body;
			bool isPlayerControlled = body.isPlayerControlled;
			if ((bool)hurtBox && hurtBox.healthComponent.alive && isPlayerControlled && !body.HasBuff(DLC2Content.Buffs.GeodeBuff))
			{
				Util.PlaySound("Play_env_meridian_geode_cleanse_debuff", body.gameObject);
				Util.CleanseBody(body, removeDebuffs: true, removeBuffs: false, removeCooldownBuffs: true, removeDots: true, removeStun: true, removeNearbyProjectiles: true);
				hurtBox.healthComponent.AddBarrier(hurtBox.healthComponent.fullHealth * geodeController.barrierToHealthPercentage);
				body.AddBuff(DLC2Content.Buffs.GeodeBuff);
			}
		}
	}

	protected void RemoveGeodeBuffFromAllPlayers()
	{
		foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
		{
			CharacterBody currentBody = instance.networkUser.GetCurrentBody();
			if (currentBody.HasBuff(DLC2Content.Buffs.GeodeBuff))
			{
				currentBody.RemoveBuff(DLC2Content.Buffs.GeodeBuff);
			}
		}
	}

	protected void SearchForPlayers(List<HurtBox> dest)
	{
		TeamMask mask = default(TeamMask);
		mask.AddTeam(TeamIndex.Player);
		sphereSearch.mask = LayerIndex.entityPrecise.mask;
		sphereSearch.origin = base.gameObject.transform.position;
		sphereSearch.radius = geodeController.geodeBuffRadius;
		sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
		sphereSearch.RefreshCandidates();
		sphereSearch.FilterCandidatesByHurtBoxTeam(mask);
		sphereSearch.OrderCandidatesByDistance();
		sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
		sphereSearch.GetHurtBoxes(dest);
		sphereSearch.ClearCandidates();
	}
}

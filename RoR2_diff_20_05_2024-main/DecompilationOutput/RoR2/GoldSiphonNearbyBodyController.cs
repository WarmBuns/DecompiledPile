using System;
using System.Collections.Generic;
using EntityStates;
using EntityStates.ShrineHalcyonite;
using HG;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class GoldSiphonNearbyBodyController : NetworkBehaviour
{
	protected class TetheredPlayers
	{
		public GameObject tetheredPlayer;

		public bool isTethered;
	}

	public GameObject shrineGameObject;

	private TeamComponent parentTeamComponent;

	public TetherVfxOrigin tetherVfxOrigin;

	public bool automaticallyScaleCostWithDifficulty = true;

	private uint appliedDrain;

	protected SphereSearch sphereSearch;

	protected float timer;

	protected new Transform transform;

	private bool isTetheredToAtLeastOneObject;

	private int tetheredPlayers;

	private HalcyoniteShrineInteractable parentShrineReference;

	protected PurchaseInteraction purchaseInteraction;

	private int goldDrainValue = 1;

	private EntityStateMachine stateMachine;

	public SerializableEntityStateType lowQualityState = new SerializableEntityStateType(typeof(ShrineHalcyoniteLowQuality));

	public SerializableEntityStateType midQualityState = new SerializableEntityStateType(typeof(ShrineHalcyoniteMidQuality));

	public SerializableEntityStateType maxQualityState = new SerializableEntityStateType(typeof(ShrineHalcyoniteMaxQuality));

	private int shrineLowGoldCost = 75;

	private int shrineMidGoldCost = 150;

	private int shrineMaxGoldCost = 300;

	private float tickRate = 5f;

	private int maxTargets = 4;

	private List<TetheredPlayers> tetheredGameObjects;

	private bool isReady;

	private int tierTracker;

	public static event Action onHalcyonShrineGoldDrain;

	protected void Awake()
	{
		parentShrineReference = GetComponentInParent<HalcyoniteShrineInteractable>();
		shrineLowGoldCost = parentShrineReference.lowGoldCost;
		shrineMidGoldCost = parentShrineReference.midGoldCost;
		shrineMaxGoldCost = parentShrineReference.maxGoldCost;
		tickRate = parentShrineReference.tickRate;
		maxTargets = parentShrineReference.maxTargets;
		goldDrainValue = parentShrineReference.goldDrainValue;
		purchaseInteraction = GetComponentInParent<PurchaseInteraction>();
		appliedDrain = (uint)(goldDrainValue * -1);
		stateMachine = GetComponentInParent<EntityStateMachine>();
		sphereSearch = new SphereSearch();
		transform = base.transform;
	}

	protected void FixedUpdate()
	{
		if (parentShrineReference.interactions > 1)
		{
			timer -= Time.fixedDeltaTime;
			if (timer <= 0f)
			{
				timer += 1f / tickRate;
				DrainGold();
			}
		}
	}

	protected void DrainGold()
	{
		if (sphereSearch == null || parentShrineReference == null || stateMachine == null || this.transform == null || purchaseInteraction == null)
		{
			return;
		}
		if (!isReady)
		{
			isReady = true;
			GrabPlayerReferences();
		}
		List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
		SearchForPlayers(list);
		List<Transform> list2 = CollectionPool<Transform, List<Transform>>.RentCollection();
		for (int i = 0; i < list.Count; i++)
		{
			HurtBox hurtBox = list[i];
			bool isPlayerControlled = hurtBox.healthComponent.body.isPlayerControlled;
			if ((bool)hurtBox && (bool)hurtBox.healthComponent && hurtBox.healthComponent.alive && hurtBox.healthComponent.body.master.money > goldDrainValue && isPlayerControlled)
			{
				Transform transform = hurtBox.healthComponent.body?.coreTransform ?? hurtBox.transform;
				list2.Add(transform);
				for (int j = 0; j < tetheredGameObjects.Count; j++)
				{
					if (tetheredGameObjects[j].tetheredPlayer != null && transform.gameObject == tetheredGameObjects[j].tetheredPlayer)
					{
						tetheredGameObjects[j].tetheredPlayer = transform.gameObject;
						tetheredGameObjects[j].isTethered = true;
					}
				}
				if (NetworkServer.active)
				{
					appliedDrain = (uint)parentShrineReference.goldDrainValue;
					CharacterMaster master = hurtBox.healthComponent.body.master;
					_ = master.money;
					master.money = (uint)Mathf.Max(0f, (float)master.money - (float)appliedDrain);
					_ = master.money;
					EffectManager.SimpleImpactEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/CoinImpact"), transform.position, Vector3.up, transmit: true);
					Util.PlaySound("Play_obj_shrineHalcyonite_goldSiphon", transform.gameObject);
					parentShrineReference.StoreDrainValue(parentShrineReference.goldDrainValue);
					GoldSiphonNearbyBodyController.onHalcyonShrineGoldDrain();
					if (parentShrineReference.goldDrained < parentShrineReference.lowGoldCost && tierTracker == 0)
					{
						tierTracker++;
						purchaseInteraction.SetAvailable(newAvailable: false);
					}
					if (parentShrineReference.goldDrained > parentShrineReference.lowGoldCost && parentShrineReference.goldDrained < parentShrineReference.midGoldCost && tierTracker == 1)
					{
						tierTracker++;
						purchaseInteraction.SetAvailable(newAvailable: true);
						stateMachine.SetNextState(EntityStateCatalog.InstantiateState(ref lowQualityState));
					}
					if (parentShrineReference.goldDrained > parentShrineReference.midGoldCost && parentShrineReference.goldDrained < parentShrineReference.maxGoldCost && tierTracker == 2)
					{
						tierTracker++;
						stateMachine.SetNextState(EntityStateCatalog.InstantiateState(ref midQualityState));
					}
					if (parentShrineReference.goldDrained >= parentShrineReference.maxGoldCost && tierTracker == 3)
					{
						tierTracker++;
						stateMachine.SetNextState(EntityStateCatalog.InstantiateState(ref maxQualityState));
					}
				}
			}
			if (list2.Count >= maxTargets)
			{
				break;
			}
		}
		isTetheredToAtLeastOneObject = (float)list2.Count > 0f;
		if ((bool)tetherVfxOrigin)
		{
			tetherVfxOrigin.SetTetheredTransforms(list2);
		}
		for (int k = 0; k < tetheredGameObjects.Count; k++)
		{
			int num = 0;
			for (int l = 0; l < list2.Count; l++)
			{
				if (tetheredGameObjects[k].tetheredPlayer != list2[l])
				{
					num++;
				}
			}
			if (num == list2.Count)
			{
				tetheredGameObjects[k].isTethered = false;
			}
		}
		if (isTetheredToAtLeastOneObject)
		{
			parentShrineReference.IsDraining(drainingActive: true);
		}
		else
		{
			parentShrineReference.IsDraining(drainingActive: false);
		}
		CollectionPool<Transform, List<Transform>>.ReturnCollection(list2);
		CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
	}

	protected void SearchForPlayers(List<HurtBox> dest)
	{
		TeamMask mask = default(TeamMask);
		mask.AddTeam(TeamIndex.Player);
		sphereSearch.mask = LayerIndex.entityPrecise.mask;
		sphereSearch.origin = base.gameObject.transform.position;
		sphereSearch.radius = parentShrineReference.radius;
		sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
		sphereSearch.RefreshCandidates();
		sphereSearch.FilterCandidatesByHurtBoxTeam(mask);
		sphereSearch.OrderCandidatesByDistance();
		sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
		sphereSearch.GetHurtBoxes(dest);
		sphereSearch.ClearCandidates();
	}

	private void GrabPlayerReferences()
	{
		tetheredGameObjects = CollectionPool<TetheredPlayers, List<TetheredPlayers>>.RentCollection();
		for (int i = 0; i < PlayerCharacterMasterController.instances.Count; i++)
		{
			PlayerCharacterMasterController playerCharacterMasterController = PlayerCharacterMasterController.instances[i];
			if ((bool)playerCharacterMasterController)
			{
				CharacterBody body = playerCharacterMasterController.master.GetBody();
				if ((bool)body)
				{
					TetheredPlayers tetheredPlayers = new TetheredPlayers();
					tetheredPlayers.tetheredPlayer = body.gameObject;
					tetheredGameObjects.Add(tetheredPlayers);
				}
			}
		}
	}

	private void UNetVersion()
	{
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

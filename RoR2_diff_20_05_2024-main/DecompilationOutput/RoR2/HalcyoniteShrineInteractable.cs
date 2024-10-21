using System.Runtime.InteropServices;
using EntityStates;
using EntityStates.ShrineHalcyonite;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class HalcyoniteShrineInteractable : NetworkBehaviour
{
	[Header("Shrine Core Variables")]
	public bool HalcyoniteShrineActive = true;

	[Tooltip("Flat max gold cost for accessing top quality. *Note, without difficulty cost scaling.")]
	public int maxGoldCost = 300;

	[Tooltip("Flat mid gold cost for accessing top quality. *Note, without difficulty cost scaling.")]
	public int midGoldCost = 150;

	[Tooltip("Minimum amount of gold required to activate boss fight and end drain. *Note, without difficulty cost scaling.")]
	public int lowGoldCost = 75;

	[SerializeField]
	[Tooltip("True if shrine gold mechanic should increase with difficulty. False if it should not.")]
	private bool automaticallyScaleCostWithDifficulty = true;

	[SerializeField]
	private GameObject shrineCollisionObject;

	[Header("Draining Gold Variables")]
	[Tooltip("Detection radius for picking up the players within range of the Halcyonite Shrine.")]
	public float radius = 30f;

	[Tooltip("Amount drained per tick.")]
	public int goldDrainValue = 1;

	[Tooltip("Total amount of gold stored in Shrine.")]
	public int goldDrained;

	[Min(float.Epsilon)]
	[Tooltip("How many ticks per second.")]
	public float tickRate = 5f;

	[SyncVar]
	[Tooltip("Maximum amount of player targets allowed.")]
	public int maxTargets = 4;

	[Tooltip("Game Object for the network game object responsible for draining the player's gold.")]
	public GameObject siphonObject;

	[HideInInspector]
	[SyncVar]
	public int interactions;

	[SerializeField]
	[Tooltip("Renderer reference for when changing target renderers for the highlight script.")]
	private Renderer shrineRenderer;

	[SyncVar]
	public bool isDraining;

	[Header("Gauge Mechanic")]
	[SerializeField]
	[Tooltip("Reference to the crystal object for changing its materials.")]
	public GameObject crystalObject;

	[SerializeField]
	[SyncVar]
	public GameObject shrineGoldBottom;

	[SerializeField]
	[SyncVar]
	public GameObject shrineGoldMid;

	[SerializeField]
	[SyncVar]
	public GameObject shrineGoldTop;

	[SerializeField]
	public Material goldMaterialController;

	[SerializeField]
	[Tooltip("Color of VFX and Crystal once the encounter is finished and reward has been dispersed.")]
	public GameObject shrineBaseObject;

	[SerializeField]
	[Tooltip("Halcyonite Shrine bubble Game Object reference.")]
	public GameObject shrineHalcyoniteBubble;

	[SerializeField]
	[Tooltip("Entity State Machine reference for handling changes to gauge mechanics for the network.")]
	public EntityStateMachine stateMachine;

	public SerializableEntityStateType noQualityState = new SerializableEntityStateType(typeof(ShrineHalcyoniteNoQuality));

	public SerializableEntityStateType finishedState = new SerializableEntityStateType(typeof(ShrineHalcyoniteFinished));

	public SfxLocator sfxLocator;

	[SerializeField]
	private PurchaseInteraction purchaseInteraction;

	[Header("Shrine Encounter and Rewards")]
	[SerializeField]
	private DirectorCard chosenDirectCard;

	[SerializeField]
	[Tooltip("Drop table dictating what rewards are available at minimum gold value.")]
	private BasicPickupDropTable halcyoniteDropTableTier1;

	[SerializeField]
	[Tooltip("Drop table dictating what rewards are available at middling gold value.")]
	private BasicPickupDropTable halcyoniteDropTableTier2;

	[SerializeField]
	[Tooltip("Drop table dictating what rewards are available at maximum gold value.")]
	private BasicPickupDropTable halcyoniteDropTableTier3;

	[SerializeField]
	[Tooltip("Use this tier to get a pickup index for the reward.  The droplet's visuals will correspond to this.")]
	private ItemTier rewardDisplayTier;

	[SerializeField]
	[Tooltip("The number of options to display when the player interacts with the reward pickup.")]
	private int rewardOptionCount;

	[SerializeField]
	[Tooltip("The prefab to use for the reward pickup.")]
	private GameObject rewardPickupPrefab;

	[SerializeField]
	[Tooltip("Where to spawn the reward droplets relative to the spawn target (the center of the safe ward).")]
	private Vector3 rewardOffset;

	[Header("Colossus Portal")]
	[SerializeField]
	public ChildLocator modelChildLocator;

	public PortalSpawner[] portalSpawners;

	[Tooltip("Combat Director managing the monster wave that spawns on Halcyon Shrine's gold drain behavior activation.")]
	public CombatDirector activationDirector;

	[Tooltip("Monster credits given to combat director to determine how many monsters to spawn.")]
	public float monsterCredit;

	[SerializeField]
	private bool scaleMonsterCreditWithDifficultyCoefficient = true;

	[SerializeField]
	private CombatDirector combatDirector;

	private int quantityIncreaseFromBuyIn = 1;

	private BasicPickupDropTable rewardDropTable;

	private Xoroshiro128Plus rng;

	[SyncVar]
	public float goldMaterialModifier = -2.2f;

	[SyncVar]
	private bool visualsDone;

	private NetworkInstanceId ___shrineGoldBottomNetId;

	private NetworkInstanceId ___shrineGoldMidNetId;

	private NetworkInstanceId ___shrineGoldTopNetId;

	public static bool isCommandEnabled => RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.Command);

	public int NetworkmaxTargets
	{
		get
		{
			return maxTargets;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref maxTargets, 1u);
		}
	}

	public int Networkinteractions
	{
		get
		{
			return interactions;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref interactions, 2u);
		}
	}

	public bool NetworkisDraining
	{
		get
		{
			return isDraining;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref isDraining, 4u);
		}
	}

	public GameObject NetworkshrineGoldBottom
	{
		get
		{
			return shrineGoldBottom;
		}
		[param: In]
		set
		{
			SetSyncVarGameObject(value, ref shrineGoldBottom, 8u, ref ___shrineGoldBottomNetId);
		}
	}

	public GameObject NetworkshrineGoldMid
	{
		get
		{
			return shrineGoldMid;
		}
		[param: In]
		set
		{
			SetSyncVarGameObject(value, ref shrineGoldMid, 16u, ref ___shrineGoldMidNetId);
		}
	}

	public GameObject NetworkshrineGoldTop
	{
		get
		{
			return shrineGoldTop;
		}
		[param: In]
		set
		{
			SetSyncVarGameObject(value, ref shrineGoldTop, 32u, ref ___shrineGoldTopNetId);
		}
	}

	public float NetworkgoldMaterialModifier
	{
		get
		{
			return goldMaterialModifier;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref goldMaterialModifier, 64u);
		}
	}

	public bool NetworkvisualsDone
	{
		get
		{
			return visualsDone;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref visualsDone, 128u);
		}
	}

	protected void Awake()
	{
		if (NetworkServer.active)
		{
			if (automaticallyScaleCostWithDifficulty)
			{
				lowGoldCost = Run.instance.GetDifficultyScaledCost(lowGoldCost);
				midGoldCost = Run.instance.GetDifficultyScaledCost(midGoldCost);
				maxGoldCost = Run.instance.GetDifficultyScaledCost(maxGoldCost);
				goldDrainValue = Run.instance.GetDifficultyScaledCost(goldDrainValue);
			}
			CalculateCredits();
			HalcyoniteShrineActive = true;
			goldMaterialController.EnableKeyword("_SLICEHEIGHT");
			combatDirector = GetComponent<CombatDirector>();
		}
	}

	private void Start()
	{
		HalcyoniteShrineActive = true;
		shrineCollisionObject.layer = LayerIndex.world.intVal;
	}

	private void CalculateCredits()
	{
		if (scaleMonsterCreditWithDifficultyCoefficient)
		{
			monsterCredit = Mathf.CeilToInt(monsterCredit * Run.instance.difficultyCoefficient);
		}
	}

	[Server]
	public void TrackInteractions()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HalcyoniteShrineInteractable::TrackInteractions()' called on client");
			return;
		}
		if (interactions == 0)
		{
			GetComponent<Highlight>().targetRenderer = shrineRenderer;
			stateMachine.SetNextStateToMain();
		}
		Networkinteractions = interactions + 1;
		if (interactions >= 3)
		{
			stateMachine.SetNextState(new ShrineHalcyoniteMaxQuality());
		}
	}

	[Server]
	public void IsDraining(bool drainingActive)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HalcyoniteShrineInteractable::IsDraining(System.Boolean)' called on client");
		}
		else
		{
			NetworkisDraining = drainingActive;
		}
	}

	public void DestroyDrainVFX()
	{
		if ((bool)siphonObject)
		{
			NetworkServer.Destroy(siphonObject);
		}
		if ((bool)shrineHalcyoniteBubble && shrineHalcyoniteBubble.activeInHierarchy)
		{
			shrineHalcyoniteBubble.SetActive(value: false);
		}
		IsDraining(drainingActive: false);
	}

	public void DrainConditionMet()
	{
		DestroyDrainVFX();
		if ((bool)purchaseInteraction && purchaseInteraction.available)
		{
			purchaseInteraction.SetAvailable(newAvailable: false);
		}
		if (goldDrained > lowGoldCost && goldDrained < midGoldCost)
		{
			quantityIncreaseFromBuyIn = 1;
			rewardDropTable = halcyoniteDropTableTier1;
			rewardOptionCount = 3;
			rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
		}
		if (goldDrained > midGoldCost && goldDrained < maxGoldCost)
		{
			quantityIncreaseFromBuyIn = 1;
			rewardDropTable = halcyoniteDropTableTier2;
			rewardOptionCount = 4;
			rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
		}
		if (goldDrained >= maxGoldCost)
		{
			quantityIncreaseFromBuyIn = 1;
			rewardDropTable = halcyoniteDropTableTier3;
			rewardOptionCount = 5;
			rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
		}
		Transform transform = modelChildLocator.FindChild("ModelBase");
		if (transform != null)
		{
			Vector3 origin = transform.position + new Vector3(0f, 16f, 0f);
			EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/HalcyonShrineTierReachVFX"), new EffectData
			{
				origin = origin
			}, transmit: false);
		}
		int difficultyLevel = (int)((float)goldDrained / 100f);
		combatDirector.HalcyoniteShrineActivation(chosenDirectCard.cost, chosenDirectCard, difficultyLevel, base.gameObject.transform);
	}

	public void StoreDrainValue(int value)
	{
		goldDrained += value;
	}

	[Server]
	public void AttemptSpawnPortal()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HalcyoniteShrineInteractable::AttemptSpawnPortal()' called on client");
			return;
		}
		PortalSpawner[] array = portalSpawners;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].AttemptSpawnPortalServer();
		}
	}

	[Server]
	public void DropRewards()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.HalcyoniteShrineInteractable::DropRewards()' called on client");
			return;
		}
		stateMachine.SetNextState(EntityStateCatalog.InstantiateState(ref finishedState));
		int participatingPlayerCount = Run.instance.participatingPlayerCount;
		if (participatingPlayerCount <= 0 || !base.gameObject || !rewardDropTable)
		{
			return;
		}
		int num = participatingPlayerCount * quantityIncreaseFromBuyIn;
		float angle = 360f / (float)num;
		Vector3 vector = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
		Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
		Vector3 position = base.gameObject.transform.position + rewardOffset;
		int num2 = 0;
		while (num2 < num)
		{
			if (isCommandEnabled)
			{
				int num3 = rewardOptionCount - 2;
				for (int i = 0; i < num3; i++)
				{
					PickupIndex pickupIndex = rewardDropTable.GenerateDrop(rng);
					GenericPickupController.CreatePickupInfo createPickupInfo = default(GenericPickupController.CreatePickupInfo);
					createPickupInfo.pickupIndex = pickupIndex;
					createPickupInfo.rotation = Quaternion.identity;
					createPickupInfo.position = position;
					GenericPickupController.CreatePickupInfo pickupInfo = createPickupInfo;
					PickupDropletController.CreatePickupDroplet(pickupInfo, pickupInfo.position, vector);
				}
			}
			else
			{
				GenericPickupController.CreatePickupInfo createPickupInfo = default(GenericPickupController.CreatePickupInfo);
				createPickupInfo.pickupIndex = PickupCatalog.FindPickupIndex(rewardDisplayTier);
				createPickupInfo.pickerOptions = PickupPickerController.GenerateOptionsFromDropTablePlusForcedStorm(rewardOptionCount, halcyoniteDropTableTier3, halcyoniteDropTableTier2, rng);
				createPickupInfo.rotation = Quaternion.identity;
				createPickupInfo.prefabOverride = rewardPickupPrefab;
				GenericPickupController.CreatePickupInfo pickupInfo2 = createPickupInfo;
				pickupInfo2.position = position;
				PickupDropletController.CreatePickupDroplet(pickupInfo2, position, vector);
			}
			num2++;
			vector = quaternion * vector;
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.WritePackedUInt32((uint)maxTargets);
			writer.WritePackedUInt32((uint)interactions);
			writer.Write(isDraining);
			writer.Write(shrineGoldBottom);
			writer.Write(shrineGoldMid);
			writer.Write(shrineGoldTop);
			writer.Write(goldMaterialModifier);
			writer.Write(visualsDone);
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
			writer.WritePackedUInt32((uint)maxTargets);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32((uint)interactions);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(isDraining);
		}
		if ((base.syncVarDirtyBits & 8u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(shrineGoldBottom);
		}
		if ((base.syncVarDirtyBits & 0x10u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(shrineGoldMid);
		}
		if ((base.syncVarDirtyBits & 0x20u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(shrineGoldTop);
		}
		if ((base.syncVarDirtyBits & 0x40u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(goldMaterialModifier);
		}
		if ((base.syncVarDirtyBits & 0x80u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(visualsDone);
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
			maxTargets = (int)reader.ReadPackedUInt32();
			interactions = (int)reader.ReadPackedUInt32();
			isDraining = reader.ReadBoolean();
			___shrineGoldBottomNetId = reader.ReadNetworkId();
			___shrineGoldMidNetId = reader.ReadNetworkId();
			___shrineGoldTopNetId = reader.ReadNetworkId();
			goldMaterialModifier = reader.ReadSingle();
			visualsDone = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			maxTargets = (int)reader.ReadPackedUInt32();
		}
		if (((uint)num & 2u) != 0)
		{
			interactions = (int)reader.ReadPackedUInt32();
		}
		if (((uint)num & 4u) != 0)
		{
			isDraining = reader.ReadBoolean();
		}
		if (((uint)num & 8u) != 0)
		{
			shrineGoldBottom = reader.ReadGameObject();
		}
		if (((uint)num & 0x10u) != 0)
		{
			shrineGoldMid = reader.ReadGameObject();
		}
		if (((uint)num & 0x20u) != 0)
		{
			shrineGoldTop = reader.ReadGameObject();
		}
		if (((uint)num & 0x40u) != 0)
		{
			goldMaterialModifier = reader.ReadSingle();
		}
		if (((uint)num & 0x80u) != 0)
		{
			visualsDone = reader.ReadBoolean();
		}
	}

	public override void PreStartClient()
	{
		if (!___shrineGoldBottomNetId.IsEmpty())
		{
			NetworkshrineGoldBottom = ClientScene.FindLocalObject(___shrineGoldBottomNetId);
		}
		if (!___shrineGoldMidNetId.IsEmpty())
		{
			NetworkshrineGoldMid = ClientScene.FindLocalObject(___shrineGoldMidNetId);
		}
		if (!___shrineGoldTopNetId.IsEmpty())
		{
			NetworkshrineGoldTop = ClientScene.FindLocalObject(___shrineGoldTopNetId);
		}
	}
}

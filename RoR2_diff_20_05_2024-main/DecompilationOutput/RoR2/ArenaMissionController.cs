using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using EntityStates;
using EntityStates.Missions.Arena.NullWard;
using RoR2.CharacterAI;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(EntityStateMachine))]
[RequireComponent(typeof(Inventory))]
public class ArenaMissionController : NetworkBehaviour
{
	public class ArenaMissionBaseState : EntityState
	{
		protected ArenaMissionController arenaMissionController => instance;
	}

	public class MissionCompleted : ArenaMissionBaseState
	{
		public override void OnEnter()
		{
			base.OnEnter();
			base.arenaMissionController.clearedEffect.SetActive(value: true);
		}
	}

	[Serializable]
	public struct MonsterItemStackData
	{
		public PickupDropTable dropTable;

		public int stacks;
	}

	[Header("Behavior Values")]
	public float baseMonsterCredit;

	public float creditMultiplierPerRound;

	public int minimumNumberToSpawnPerMonsterType;

	public int totalRoundsMax;

	public int maximumNumberToSpawnBeforeSkipping;

	public float spawnDistanceMultiplier;

	public float eliteBias;

	[Header("Cached Components")]
	public GameObject[] nullWards;

	public GameObject monsterSpawnPosition;

	public GameObject rewardSpawnPosition;

	public CombatDirector[] combatDirectors;

	public GameObject clearedEffect;

	public GameObject killEffectPrefab;

	public GameObject fogDamagePrefab;

	public PortalSpawner[] completionPortalSpawners;

	public MonsterItemStackData[] monsterItemStackOrder;

	public PickupDropTable[] playerRewardOrder;

	[SerializeField]
	private int numRewardOptions;

	[SerializeField]
	private GameObject pickupPrefab;

	private EntityStateMachine mainStateMachine;

	private Xoroshiro128Plus rng;

	private List<DirectorCard> activeMonsterCards = new List<DirectorCard>();

	public readonly SyncListInt syncActiveMonsterBodies = new SyncListInt();

	private WeightedSelection<DirectorCard> availableMonsterCards;

	private float cachedDifficultyCoefficient;

	[SyncVar]
	private int _clearedRounds;

	private int nextItemStackIndex;

	private GameObject fogDamageInstance;

	private static int kListsyncActiveMonsterBodies;

	public int currentRound { get; private set; }

	public int clearedRounds
	{
		get
		{
			return _clearedRounds;
		}
		private set
		{
			Network_clearedRounds = value;
		}
	}

	public static ArenaMissionController instance { get; private set; }

	private float creditsThisRound => (baseMonsterCredit + creditMultiplierPerRound * (float)(currentRound - 1)) * cachedDifficultyCoefficient;

	public Inventory inventory { get; private set; }

	public int Network_clearedRounds
	{
		get
		{
			return _clearedRounds;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _clearedRounds, 2u);
		}
	}

	public static event Action onBeatArena;

	public static event Action onInstanceChangedGlobal;

	private void Awake()
	{
		mainStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "Main");
		inventory = GetComponent<Inventory>();
		syncActiveMonsterBodies.InitializeBehaviour(this, kListsyncActiveMonsterBodies);
	}

	private void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
		ArenaMissionController.onInstanceChangedGlobal?.Invoke();
		SceneDirector.onPreGeneratePlayerSpawnPointsServer += OnPreGeneratePlayerSpawnPointsServer;
	}

	private void OnDisable()
	{
		SceneDirector.onPreGeneratePlayerSpawnPointsServer -= OnPreGeneratePlayerSpawnPointsServer;
		instance = SingletonHelper.Unassign(instance, this);
		ArenaMissionController.onInstanceChangedGlobal?.Invoke();
	}

	private void OnPreGeneratePlayerSpawnPointsServer(SceneDirector sceneDirector, ref Action generationMethod)
	{
		generationMethod = GeneratePlayerSpawnPointsServer;
	}

	private void GeneratePlayerSpawnPointsServer()
	{
		if (nullWards.Length == 0)
		{
			return;
		}
		Vector3 position = nullWards[0].transform.position;
		NodeGraph groundNodes = SceneInfo.instance.groundNodes;
		NodeGraphSpider nodeGraphSpider = new NodeGraphSpider(SceneInfo.instance.groundNodes, HullMask.Human);
		nodeGraphSpider.AddNodeForNextStep(groundNodes.FindClosestNode(position, HullClassification.Human));
		for (int i = 0; i < 4; i++)
		{
			nodeGraphSpider.PerformStep();
			if (nodeGraphSpider.collectedSteps.Count > 16)
			{
				break;
			}
		}
		for (int j = 0; j < nodeGraphSpider.collectedSteps.Count; j++)
		{
			NodeGraphSpider.StepInfo stepInfo = nodeGraphSpider.collectedSteps[j];
			SpawnPoint.AddSpawnPoint(groundNodes, stepInfo.node, rng);
		}
	}

	[Server]
	public override void OnStartServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ArenaMissionController::OnStartServer()' called on client");
			return;
		}
		base.OnStartServer();
		fogDamageInstance = UnityEngine.Object.Instantiate(fogDamagePrefab);
		FogDamageController component = fogDamageInstance.GetComponent<FogDamageController>();
		GameObject[] array = nullWards;
		foreach (GameObject gameObject in array)
		{
			component.AddSafeZone(gameObject.GetComponent<SphereZone>());
		}
		NetworkServer.Spawn(fogDamageInstance);
		cachedDifficultyCoefficient = Run.instance.difficultyCoefficient;
		rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUint);
		InitCombatDirectors();
		Util.ShuffleArray(nullWards, rng);
		ReadyNextNullWard();
		availableMonsterCards = Util.CreateReasonableDirectorCardSpawnList(baseMonsterCredit * cachedDifficultyCoefficient, maximumNumberToSpawnBeforeSkipping, minimumNumberToSpawnPerMonsterType);
		_ = availableMonsterCards.Count;
	}

	[Server]
	private void ReadyNextNullWard()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ArenaMissionController::ReadyNextNullWard()' called on client");
			return;
		}
		if (currentRound > nullWards.Length)
		{
			Debug.LogError("Out of null wards! Aborting.");
			return;
		}
		EntityStateMachine component = nullWards[currentRound].GetComponent<EntityStateMachine>();
		component.initialStateType = new SerializableEntityStateType(typeof(WardOnAndReady));
		component.SetNextState(new WardOnAndReady());
	}

	[Server]
	private void InitCombatDirectors()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ArenaMissionController::InitCombatDirectors()' called on client");
			return;
		}
		for (int i = 0; i < combatDirectors.Length; i++)
		{
			CombatDirector obj = combatDirectors[i];
			obj.maximumNumberToSpawnBeforeSkipping = maximumNumberToSpawnBeforeSkipping;
			obj.onSpawnedServer.AddListener(ModifySpawnedMasters);
			obj.spawnDistanceMultiplier = spawnDistanceMultiplier;
			obj.eliteBias = eliteBias;
		}
	}

	[Server]
	public void BeginRound()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ArenaMissionController::BeginRound()' called on client");
			return;
		}
		currentRound++;
		switch (currentRound)
		{
		case 1:
			AddMonsterType();
			break;
		case 2:
			AddItemStack();
			break;
		case 3:
			AddMonsterType();
			break;
		case 4:
			AddItemStack();
			break;
		case 5:
			AddMonsterType();
			break;
		case 6:
			AddItemStack();
			break;
		case 7:
			AddMonsterType();
			break;
		case 8:
			AddItemStack();
			break;
		case 9:
			AddItemStack();
			break;
		}
		int count = activeMonsterCards.Count;
		for (int i = 0; i < count; i++)
		{
			DirectorCard directorCard = activeMonsterCards[i];
			float num = creditsThisRound / (float)count;
			float creditMultiplier = creditMultiplierPerRound * (float)currentRound / (float)count;
			if (i > combatDirectors.Length)
			{
				Debug.LogError("Trying to activate more combat directors than available. Aborting.");
				break;
			}
			CombatDirector obj = combatDirectors[i];
			obj.monsterCredit += num;
			obj.creditMultiplier = creditMultiplier;
			obj.currentSpawnTarget = monsterSpawnPosition;
			obj.OverrideCurrentMonsterCard(directorCard);
			obj.monsterSpawnTimer = 0f;
			obj.enabled = true;
			Debug.LogFormat("Enabling director {0} with {1} credits to spawn {2}", i, num, directorCard.spawnCard.name);
		}
	}

	[Server]
	public void ModifySpawnedMasters(GameObject targetGameObject)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ArenaMissionController::ModifySpawnedMasters(UnityEngine.GameObject)' called on client");
			return;
		}
		CharacterMaster component = targetGameObject.GetComponent<CharacterMaster>();
		BaseAI ai = component.GetComponent<BaseAI>();
		if ((bool)ai)
		{
			ai.onBodyDiscovered += OnBodyDiscovered;
		}
		CharacterBody body = component.GetBody();
		if ((bool)body)
		{
			EntityStateMachine[] components = body.GetComponents<EntityStateMachine>();
			foreach (EntityStateMachine obj in components)
			{
				obj.initialStateType = obj.mainStateType;
			}
		}
		component.inventory.AddItemsFrom(inventory);
		void OnBodyDiscovered(CharacterBody newBody)
		{
			ai.ForceAcquireNearestEnemyIfNoCurrentEnemy();
			ai.onBodyDiscovered -= OnBodyDiscovered;
		}
	}

	[Server]
	public void EndRound()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ArenaMissionController::EndRound()' called on client");
			return;
		}
		clearedRounds++;
		if (currentRound < totalRoundsMax)
		{
			ReadyNextNullWard();
		}
		else
		{
			if ((bool)fogDamageInstance)
			{
				UnityEngine.Object.Destroy(fogDamageInstance);
				fogDamageInstance = null;
			}
			ArenaMissionController.onBeatArena?.Invoke();
			mainStateMachine.SetNextState(new MissionCompleted());
			Chat.SendBroadcastChat(new Chat.SimpleChatMessage
			{
				baseToken = "ARENA_END"
			});
			PortalSpawner[] array = completionPortalSpawners;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].AttemptSpawnPortalServer();
			}
		}
		for (int j = 0; j < combatDirectors.Length; j++)
		{
			CombatDirector obj = combatDirectors[j];
			obj.enabled = false;
			obj.monsterCredit = 0f;
		}
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(TeamIndex.Monster);
		for (int num = teamMembers.Count - 1; num >= 0; num--)
		{
			teamMembers[num].body.healthComponent.Suicide(base.gameObject, base.gameObject, DamageType.VoidDeath);
		}
		int participatingPlayerCount = Run.instance.participatingPlayerCount;
		if (participatingPlayerCount == 0 || !rewardSpawnPosition)
		{
			return;
		}
		PickupIndex[] array2 = Array.Empty<PickupIndex>();
		int num2 = currentRound - 1;
		if (num2 < playerRewardOrder.Length)
		{
			array2 = playerRewardOrder[num2]?.GenerateUniqueDrops(numRewardOptions, rng);
		}
		if (array2.Length != 0)
		{
			ItemTier itemTier = PickupCatalog.GetPickupDef(array2[0]).itemTier;
			int num3 = participatingPlayerCount;
			float angle = 360f / (float)num3;
			Vector3 vector = Quaternion.AngleAxis(UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
			Quaternion quaternion = Quaternion.AngleAxis(angle, Vector3.up);
			int num4 = 0;
			while (num4 < num3)
			{
				GenericPickupController.CreatePickupInfo createPickupInfo = default(GenericPickupController.CreatePickupInfo);
				createPickupInfo.pickerOptions = PickupPickerController.GenerateOptionsFromArray(array2);
				createPickupInfo.prefabOverride = pickupPrefab;
				createPickupInfo.position = rewardSpawnPosition.transform.position;
				createPickupInfo.rotation = Quaternion.identity;
				createPickupInfo.pickupIndex = PickupCatalog.FindPickupIndex(itemTier);
				GenericPickupController.CreatePickupInfo pickupInfo = createPickupInfo;
				PickupDropletController.CreatePickupDroplet(pickupInfo, pickupInfo.position, vector);
				num4++;
				vector = quaternion * vector;
			}
		}
	}

	[Server]
	private void AddMonsterType()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ArenaMissionController::AddMonsterType()' called on client");
		}
		else if (availableMonsterCards.Count != 0)
		{
			int num = availableMonsterCards.EvaluateToChoiceIndex(rng.nextNormalizedFloat);
			DirectorCard value = availableMonsterCards.choices[num].value;
			activeMonsterCards.Add(value);
			syncActiveMonsterBodies.Add((int)(value.spawnCard.prefab.GetComponent<CharacterMaster>()?.bodyPrefab.GetComponent<CharacterBody>()?.bodyIndex ?? BodyIndex.None));
			availableMonsterCards.RemoveChoice(num);
			CharacterBody component = value.spawnCard.prefab.GetComponent<CharacterMaster>().bodyPrefab.GetComponent<CharacterBody>();
			Chat.SubjectFormatChatMessage subjectFormatChatMessage = new Chat.SubjectFormatChatMessage();
			subjectFormatChatMessage.baseToken = "ARENA_ADD_MONSTER";
			subjectFormatChatMessage.paramTokens = new string[1] { component.baseNameToken };
			Chat.SendBroadcastChat(subjectFormatChatMessage);
		}
	}

	[Server]
	private void AddItemStack()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.ArenaMissionController::AddItemStack()' called on client");
			return;
		}
		PickupIndex pickupIndex = PickupIndex.none;
		if (nextItemStackIndex < monsterItemStackOrder.Length)
		{
			PickupDropTable dropTable = monsterItemStackOrder[nextItemStackIndex].dropTable;
			if ((bool)dropTable)
			{
				pickupIndex = dropTable.GenerateDrop(rng);
			}
		}
		if (pickupIndex != PickupIndex.none)
		{
			PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
			ItemIndex itemIndex = pickupDef.itemIndex;
			inventory.GiveItem(itemIndex, monsterItemStackOrder[nextItemStackIndex].stacks);
			Chat.SendBroadcastChat(new Chat.PlayerPickupChatMessage
			{
				baseToken = "ARENA_ADD_ITEM",
				pickupToken = pickupDef.nameToken,
				pickupColor = pickupDef.baseColor
			});
		}
		nextItemStackIndex++;
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeSyncListsyncActiveMonsterBodies(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("SyncList syncActiveMonsterBodies called on server.");
		}
		else
		{
			((ArenaMissionController)obj).syncActiveMonsterBodies.HandleMsg(reader);
		}
	}

	static ArenaMissionController()
	{
		kListsyncActiveMonsterBodies = 1496902198;
		NetworkBehaviour.RegisterSyncListDelegate(typeof(ArenaMissionController), kListsyncActiveMonsterBodies, InvokeSyncListsyncActiveMonsterBodies);
		NetworkCRC.RegisterBehaviour("ArenaMissionController", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			SyncListInt.WriteInstance(writer, syncActiveMonsterBodies);
			writer.WritePackedUInt32((uint)_clearedRounds);
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
			SyncListInt.WriteInstance(writer, syncActiveMonsterBodies);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32((uint)_clearedRounds);
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
			SyncListInt.ReadReference(reader, syncActiveMonsterBodies);
			_clearedRounds = (int)reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			SyncListInt.ReadReference(reader, syncActiveMonsterBodies);
		}
		if (((uint)num & 2u) != 0)
		{
			_clearedRounds = (int)reader.ReadPackedUInt32();
		}
	}

	public override void PreStartClient()
	{
	}
}

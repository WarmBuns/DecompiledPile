using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using HG;
using RoR2.CharacterAI;
using RoR2.ConVar;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class Stage : NetworkBehaviour
{
	[SyncVar]
	private float _entryTime;

	[SyncVar]
	private float _entryStopwatchValue;

	[SyncVar]
	private float _entryDifficultyCoefficient;

	[SyncVar]
	private int _singleMonsterTypeBodyIndex = -1;

	private bool spawnedAnyPlayer;

	[NonSerialized]
	public bool usePod = (bool)Run.instance && Run.instance.spawnWithPod && stage1PodConVar.value;

	private static BoolConVar stage1PodConVar = new BoolConVar("stage1_pod", ConVarFlags.Cheat, "1", "Whether or not to use the pod when spawning on the first stage.");

	[SyncVar]
	private float _stageAdvanceTime = float.PositiveInfinity;

	public const float stageAdvanceTransitionDuration = 0.5f;

	public const float stageAdvanceTransitionDelay = 0.75f;

	private SceneDef nextStage;

	public static Stage instance { get; private set; }

	public Run.FixedTimeStamp entryTime
	{
		get
		{
			return Run.FixedTimeStamp.zero + _entryTime;
		}
		private set
		{
			Network_entryTime = value - Run.FixedTimeStamp.zero;
		}
	}

	public float entryStopwatchValue
	{
		get
		{
			return _entryStopwatchValue;
		}
		private set
		{
			Network_entryStopwatchValue = value;
		}
	}

	public float entryDifficultyCoefficient
	{
		get
		{
			return _entryDifficultyCoefficient;
		}
		private set
		{
			Network_entryDifficultyCoefficient = value;
		}
	}

	public BodyIndex singleMonsterTypeBodyIndex
	{
		get
		{
			return (BodyIndex)_singleMonsterTypeBodyIndex;
		}
		set
		{
			Network_singleMonsterTypeBodyIndex = (int)value;
		}
	}

	public SceneDef sceneDef { get; private set; }

	public Run.FixedTimeStamp stageAdvanceTime
	{
		get
		{
			return Run.FixedTimeStamp.zero + _stageAdvanceTime;
		}
		private set
		{
			Network_stageAdvanceTime = value - Run.FixedTimeStamp.zero;
		}
	}

	public bool completed { get; private set; }

	public bool scavPackDroppedServer { get; set; }

	public float Network_entryTime
	{
		get
		{
			return _entryTime;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _entryTime, 1u);
		}
	}

	public float Network_entryStopwatchValue
	{
		get
		{
			return _entryStopwatchValue;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _entryStopwatchValue, 2u);
		}
	}

	public float Network_entryDifficultyCoefficient
	{
		get
		{
			return _entryDifficultyCoefficient;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _entryDifficultyCoefficient, 4u);
		}
	}

	public int Network_singleMonsterTypeBodyIndex
	{
		get
		{
			return _singleMonsterTypeBodyIndex;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _singleMonsterTypeBodyIndex, 8u);
		}
	}

	public float Network_stageAdvanceTime
	{
		get
		{
			return _stageAdvanceTime;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _stageAdvanceTime, 16u);
		}
	}

	public static event Action<Stage> onServerStageBegin;

	public static event Action<Stage> onServerStageComplete;

	public static event Action<Stage> onStageStartGlobal;

	private IEnumerator Start()
	{
		sceneDef = SceneCatalog.GetSceneDefForCurrentScene();
		if (NetworkServer.active)
		{
			entryTime = Run.FixedTimeStamp.now;
			entryStopwatchValue = Run.instance.GetRunStopwatch();
			entryDifficultyCoefficient = Run.instance.difficultyCoefficient;
			RespawnAllNPCs();
			BeginServer();
		}
		if (NetworkClient.active)
		{
			yield return new WaitUntil(() => NetworkUser.readOnlyInstancesList.Any((NetworkUser x) => x.isLocalPlayer));
			RespawnLocalPlayers();
		}
		Stage.onStageStartGlobal?.Invoke(this);
	}

	private void RespawnAllNPCs()
	{
		if (sceneDef.suppressNpcEntry)
		{
			return;
		}
		if (sceneDef.needSkipDevotionRespawn)
		{
			DevotionInventoryController.StartRespawnAllLemurians();
		}
		ReadOnlyCollection<CharacterMaster> readOnlyInstancesList = CharacterMaster.readOnlyInstancesList;
		Transform playerSpawnTransform = GetPlayerSpawnTransform();
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			CharacterMaster characterMaster = readOnlyInstancesList[i];
			if ((sceneDef.needSkipDevotionRespawn && readOnlyInstancesList[i].GetComponent<DevotedLemurianController>() != null) || !characterMaster || (bool)characterMaster.GetComponent<PlayerCharacterMasterController>() || (bool)characterMaster.GetBodyObject() || !Util.IsDontDestroyOnLoad(characterMaster.gameObject))
			{
				continue;
			}
			Vector3 position = Vector3.zero;
			Quaternion rotation = Quaternion.identity;
			if ((bool)playerSpawnTransform)
			{
				position = playerSpawnTransform.position;
				rotation = playerSpawnTransform.rotation;
				BaseAI component = readOnlyInstancesList[i].GetComponent<BaseAI>();
				CharacterBody component2 = readOnlyInstancesList[i].bodyPrefab.GetComponent<CharacterBody>();
				if ((bool)component && (bool)component2)
				{
					NodeGraph desiredSpawnNodeGraph = component.GetDesiredSpawnNodeGraph();
					if ((bool)desiredSpawnNodeGraph)
					{
						List<NodeGraph.NodeIndex> list = CollectionPool<NodeGraph.NodeIndex, List<NodeGraph.NodeIndex>>.RentCollection();
						desiredSpawnNodeGraph.FindNodesInRange(position, 10f, 100f, (HullMask)(1 << (int)component2.hullClassification), list);
						if ((float)list.Count > 0f)
						{
							desiredSpawnNodeGraph.GetNodePosition(list[UnityEngine.Random.Range(0, list.Count)], out position);
						}
						list = CollectionPool<NodeGraph.NodeIndex, List<NodeGraph.NodeIndex>>.ReturnCollection(list);
					}
				}
			}
			readOnlyInstancesList[i].Respawn(position, rotation, wasRevivedMidStage: true);
		}
	}

	[Client]
	public void RespawnLocalPlayers()
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void RoR2.Stage::RespawnLocalPlayers()' called on server");
		}
		else
		{
			if (sceneDef.suppressPlayerEntry)
			{
				return;
			}
			ReadOnlyCollection<NetworkUser> readOnlyInstancesList = NetworkUser.readOnlyInstancesList;
			for (int i = 0; i < readOnlyInstancesList.Count; i++)
			{
				NetworkUser networkUser = readOnlyInstancesList[i];
				CharacterMaster characterMaster = null;
				if (networkUser.isLocalPlayer && (bool)networkUser.masterObject)
				{
					characterMaster = networkUser.masterObject.GetComponent<CharacterMaster>();
				}
				if ((bool)characterMaster)
				{
					characterMaster.CallCmdRespawn("");
				}
			}
		}
	}

	private void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
	}

	private void OnDisable()
	{
		instance = SingletonHelper.Unassign(instance, this);
	}

	[Server]
	public Transform GetPlayerSpawnTransform()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'UnityEngine.Transform RoR2.Stage::GetPlayerSpawnTransform()' called on client");
			return null;
		}
		SpawnPoint spawnPoint = SpawnPoint.ConsumeSpawnPoint();
		if ((bool)spawnPoint)
		{
			return spawnPoint.transform;
		}
		return null;
	}

	[Server]
	public void RespawnCharacter(CharacterMaster characterMaster)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Stage::RespawnCharacter(RoR2.CharacterMaster)' called on client");
		}
		else if ((bool)characterMaster)
		{
			Transform playerSpawnTransform = GetPlayerSpawnTransform();
			Vector3 vector = Vector3.zero;
			Quaternion quaternion = Quaternion.identity;
			if ((bool)playerSpawnTransform)
			{
				vector = playerSpawnTransform.position;
				quaternion = playerSpawnTransform.rotation;
			}
			characterMaster.Respawn(vector, quaternion);
			if ((bool)characterMaster.GetComponent<PlayerCharacterMasterController>())
			{
				spawnedAnyPlayer = true;
			}
			if (usePod)
			{
				Run.instance.HandlePlayerFirstEntryAnimation(characterMaster.GetBody(), vector, quaternion);
			}
		}
	}

	[Server]
	public void BeginAdvanceStage(SceneDef destinationStage)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Stage::BeginAdvanceStage(RoR2.SceneDef)' called on client");
			return;
		}
		stageAdvanceTime = Run.FixedTimeStamp.now + 0.75f;
		nextStage = destinationStage;
	}

	[Server]
	private void BeginServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Stage::BeginServer()' called on client");
		}
		else
		{
			Stage.onServerStageBegin?.Invoke(this);
		}
	}

	[Server]
	public void CompleteServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.Stage::CompleteServer()' called on client");
		}
		else if (!completed)
		{
			completed = true;
			Stage.onServerStageComplete?.Invoke(this);
		}
	}

	private void FixedUpdate()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		if ((bool)nextStage && stageAdvanceTime.hasPassed)
		{
			SceneDef nextScene = nextStage;
			nextStage = null;
			Run.instance.AdvanceStage(nextScene);
		}
		if (!spawnedAnyPlayer || !stageAdvanceTime.isInfinity || Run.instance.isGameOverServer)
		{
			return;
		}
		ReadOnlyCollection<PlayerCharacterMasterController> instances = PlayerCharacterMasterController.instances;
		bool flag = false;
		for (int i = 0; i < instances.Count; i++)
		{
			PlayerCharacterMasterController playerCharacterMasterController = instances[i];
			if (playerCharacterMasterController.isConnected && playerCharacterMasterController.preventGameOver)
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			Run.instance.BeginGameOver(RoR2Content.GameEndings.StandardLoss);
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(_entryTime);
			writer.Write(_entryStopwatchValue);
			writer.Write(_entryDifficultyCoefficient);
			writer.WritePackedUInt32((uint)_singleMonsterTypeBodyIndex);
			writer.Write(_stageAdvanceTime);
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
			writer.Write(_entryTime);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_entryStopwatchValue);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_entryDifficultyCoefficient);
		}
		if ((base.syncVarDirtyBits & 8u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32((uint)_singleMonsterTypeBodyIndex);
		}
		if ((base.syncVarDirtyBits & 0x10u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_stageAdvanceTime);
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
			_entryTime = reader.ReadSingle();
			_entryStopwatchValue = reader.ReadSingle();
			_entryDifficultyCoefficient = reader.ReadSingle();
			_singleMonsterTypeBodyIndex = (int)reader.ReadPackedUInt32();
			_stageAdvanceTime = reader.ReadSingle();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			_entryTime = reader.ReadSingle();
		}
		if (((uint)num & 2u) != 0)
		{
			_entryStopwatchValue = reader.ReadSingle();
		}
		if (((uint)num & 4u) != 0)
		{
			_entryDifficultyCoefficient = reader.ReadSingle();
		}
		if (((uint)num & 8u) != 0)
		{
			_singleMonsterTypeBodyIndex = (int)reader.ReadPackedUInt32();
		}
		if (((uint)num & 0x10u) != 0)
		{
			_stageAdvanceTime = reader.ReadSingle();
		}
	}

	public override void PreStartClient()
	{
	}
}

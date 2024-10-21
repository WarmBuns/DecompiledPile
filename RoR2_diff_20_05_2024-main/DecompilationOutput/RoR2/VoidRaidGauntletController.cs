using System;
using System.Runtime.InteropServices;
using RoR2.Audio;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class VoidRaidGauntletController : NetworkBehaviour
{
	[Serializable]
	public class GauntletInfo
	{
		public Transform startPoint;

		public Transform effectRoot;

		public float startRadius = 100f;

		public MapZone exitZone;

		public MapZone entranceZone;

		public string gateName;
	}

	[Serializable]
	public class DonutInfo
	{
		public GameObject root;

		public Transform returnPoint;

		public float returnRadius = 1000f;

		public Transform crabPosition;

		public CombatDirector combatDirector;
	}

	[SerializeField]
	private DonutInfo initialDonut;

	[SerializeField]
	private DonutInfo[] followingDonuts;

	[SerializeField]
	private GauntletInfo[] gauntlets;

	[SerializeField]
	private BuffDef requiredBuffToKill;

	[SerializeField]
	private GameObject donutSkyboxObject;

	[SerializeField]
	private GameObject gauntletSkyboxObject;

	[SerializeField]
	private GameObject gauntletEffectPrefab;

	[SerializeField]
	private int initialSpawnSpiderSteps = 4;

	[SerializeField]
	private int maxInitialSpawnPoints = 16;

	[SerializeField]
	public InteractableSpawnCard outroPortalSpawnCard;

	[SerializeField]
	public float minOutroPortalDistance;

	[SerializeField]
	public float maxOutroPortalDistance;

	[SerializeField]
	private InteractableSpawnCard gauntletExtranceSpawnCard;

	[SerializeField]
	private ScriptedCombatEncounter[] phaseEncounters;

	[SerializeField]
	private LoopSoundDef gauntletActiveLoop;

	private GauntletInfo previousGauntlet;

	private DonutInfo previousDonut;

	private GauntletInfo currentGauntlet;

	private DonutInfo currentDonut;

	private CharacterBody bossBody;

	private LoopSoundManager.SoundLoopPtr gauntletActiveLoopPtr;

	[SyncVar(hook = "TryShuffleData")]
	private ulong rngSeed;

	private bool hasShuffled;

	private int gauntletIndex;

	private static int kRpcRpcStartActiveSoundLoop;

	private static int kRpcRpcActivateGate;

	private static int kRpcRpcActivateDonut;

	private static int kRpcRpcTryShuffleData;

	public static VoidRaidGauntletController instance { get; private set; }

	public ulong NetworkrngSeed
	{
		get
		{
			return rngSeed;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				TryShuffleData(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref rngSeed, 1u);
		}
	}

	public void SetCurrentDonutCombatDirectorEnabled(bool isEnabled)
	{
		if ((bool)currentDonut?.combatDirector)
		{
			currentDonut.combatDirector.enabled = isEnabled;
		}
	}

	public bool TryOpenGauntlet(Vector3 entrancePosition, NetworkInstanceId bossMasterId)
	{
		if (gauntletIndex >= phaseEncounters.Length)
		{
			return false;
		}
		if (bossMasterId != NetworkInstanceId.Invalid)
		{
			ScriptedCombatEncounter scriptedCombatEncounter = phaseEncounters[gauntletIndex];
			if (!scriptedCombatEncounter || scriptedCombatEncounter.combatSquad.memberCount != 0 || !scriptedCombatEncounter.combatSquad.HasContainedMember(bossMasterId))
			{
				return false;
			}
		}
		int destinationGauntletIndex = gauntletIndex;
		previousGauntlet = currentGauntlet;
		previousDonut = currentDonut;
		if (previousDonut != null && (bool)previousDonut.combatDirector)
		{
			previousDonut.combatDirector.monsterCredit = 0f;
			previousDonut.combatDirector.enabled = false;
		}
		int num = gauntletIndex % followingDonuts.Length;
		currentDonut = followingDonuts[num];
		currentGauntlet = gauntlets[gauntletIndex % gauntlets.Length];
		gauntletIndex++;
		CallRpcTryShuffleData(rngSeed);
		if ((bool)currentDonut.root)
		{
			currentDonut.root.SetActive(value: true);
			CallRpcActivateDonut(num);
		}
		if (SceneInfo.instance != null && !string.IsNullOrEmpty(currentGauntlet?.gateName))
		{
			SceneInfo.instance.SetGateState(currentGauntlet.gateName, gateEnabled: true);
			CallRpcActivateGate(currentGauntlet?.gateName);
		}
		if ((bool)currentGauntlet.effectRoot && (bool)gauntletEffectPrefab)
		{
			EffectData effectData = new EffectData
			{
				origin = currentGauntlet.effectRoot.position,
				rotation = currentGauntlet.effectRoot.rotation
			};
			EffectManager.SpawnEffect(gauntletEffectPrefab, effectData, transmit: false);
		}
		if ((bool)previousDonut?.combatDirector)
		{
			previousDonut.combatDirector.enabled = false;
		}
		Xoroshiro128Plus rng = new Xoroshiro128Plus(rngSeed + (ulong)gauntletIndex);
		DirectorPlacementRule placementRule = new DirectorPlacementRule
		{
			placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
			position = entrancePosition
		};
		DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(gauntletExtranceSpawnCard, placementRule, rng);
		directorSpawnRequest.onSpawnedServer = delegate(SpawnCard.SpawnResult result)
		{
			OnEntranceSpawned(result, destinationGauntletIndex);
		};
		DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
		return true;
	}

	private void OnEntranceSpawned(SpawnCard.SpawnResult result, int destinationGauntletIndex)
	{
		if (result.success)
		{
			CallRpcStartActiveSoundLoop();
			currentGauntlet.entranceZone = result.spawnedInstance.GetComponentInChildren<MapZone>();
			result.spawnedInstance.GetComponent<VoidRaidGauntletEntranceController>().SetGauntletIndex(destinationGauntletIndex);
		}
	}

	private void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
		SceneDirector.onPreGeneratePlayerSpawnPointsServer += OnPreGeneratePlayerSpawnPointsServer;
	}

	private void OnDisable()
	{
		StopActiveSoundLoop();
		SceneDirector.onPreGeneratePlayerSpawnPointsServer -= OnPreGeneratePlayerSpawnPointsServer;
		instance = SingletonHelper.Unassign(instance, this);
	}

	private void OnPreGeneratePlayerSpawnPointsServer(SceneDirector sceneDirector, ref Action generationMethod)
	{
		generationMethod = GeneratePlayerSpawnPointsServer;
	}

	private void GeneratePlayerSpawnPointsServer()
	{
		if (!SceneInfo.instance || initialDonut == null || !initialDonut.returnPoint)
		{
			return;
		}
		Transform returnPoint = initialDonut.returnPoint;
		Vector3 position = returnPoint.position;
		NodeGraph groundNodes = SceneInfo.instance.groundNodes;
		if (!groundNodes)
		{
			Debug.LogError("VoidRaidGauntletController.GeneratePlayerSpawnPointsServer: No ground nodegraph found to place spawn points.", this);
			return;
		}
		NodeGraphSpider nodeGraphSpider = new NodeGraphSpider(groundNodes, HullMask.Human);
		nodeGraphSpider.AddNodeForNextStep(groundNodes.FindClosestNode(position, HullClassification.Human));
		for (int i = 0; i < initialSpawnSpiderSteps; i++)
		{
			nodeGraphSpider.PerformStep();
			if (nodeGraphSpider.collectedSteps.Count > maxInitialSpawnPoints)
			{
				break;
			}
		}
		for (int j = 0; j < nodeGraphSpider.collectedSteps.Count; j++)
		{
			NodeGraphSpider.StepInfo stepInfo = nodeGraphSpider.collectedSteps[j];
			groundNodes.GetNodePosition(stepInfo.node, out var position2);
			Quaternion rotation = returnPoint.rotation;
			SpawnPoint.AddSpawnPoint(position2, rotation);
		}
	}

	private void Start()
	{
		previousDonut = initialDonut;
		DonutInfo[] array = followingDonuts;
		foreach (DonutInfo donutInfo in array)
		{
			if ((bool)donutInfo?.root)
			{
				donutInfo.root.SetActive(value: false);
			}
		}
		if (NetworkServer.active)
		{
			TryShuffleData(Run.instance.stageRng.nextUlong);
			CallRpcTryShuffleData(rngSeed);
		}
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		CallRpcTryShuffleData(rngSeed);
	}

	private void TryShuffleData(ulong seed)
	{
		NetworkrngSeed = seed;
		if (hasShuffled)
		{
			return;
		}
		Xoroshiro128Plus rng = new Xoroshiro128Plus(seed);
		Util.ShuffleArray(followingDonuts, rng);
		Util.ShuffleArray(gauntlets, rng);
		for (int i = 1; i < phaseEncounters.Length && i - 1 < followingDonuts.Length; i++)
		{
			DonutInfo donutInfo = followingDonuts[i - 1];
			ScriptedCombatEncounter scriptedCombatEncounter = phaseEncounters[i];
			int encounterIndex = i;
			scriptedCombatEncounter.onBeginEncounter += delegate(ScriptedCombatEncounter argEncounter)
			{
				OnBeginEncounter(argEncounter, encounterIndex);
			};
			if (scriptedCombatEncounter.spawns.Length != 0)
			{
				scriptedCombatEncounter.spawns[0].explicitSpawnPosition = donutInfo.crabPosition;
			}
		}
		for (int j = 0; j < gauntlets.Length && j < followingDonuts.Length; j++)
		{
			GauntletInfo gauntletInfo = gauntlets[j];
			DonutInfo donutInfo2 = followingDonuts[j];
			if ((bool)gauntletInfo.exitZone && (bool)donutInfo2.returnPoint)
			{
				gauntletInfo.exitZone.explicitDestination = donutInfo2.returnPoint;
				gauntletInfo.exitZone.destinationIdealRadius = donutInfo2.returnRadius;
			}
		}
		hasShuffled = true;
	}

	private void OnBeginEncounter(ScriptedCombatEncounter encounter, int encounterIndex)
	{
		while (gauntletIndex < encounterIndex)
		{
			TryOpenGauntlet(currentDonut.crabPosition.position, NetworkInstanceId.Invalid);
		}
	}

	public void SpawnOutroPortal()
	{
		if (NetworkServer.active && currentDonut != null && (bool)currentDonut.returnPoint)
		{
			Xoroshiro128Plus rng = new Xoroshiro128Plus(rngSeed + 1);
			DirectorPlacementRule placementRule = new DirectorPlacementRule
			{
				placementMode = DirectorPlacementRule.PlacementMode.Approximate,
				minDistance = minOutroPortalDistance,
				maxDistance = maxOutroPortalDistance,
				spawnOnTarget = currentDonut.returnPoint
			};
			DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(outroPortalSpawnCard, placementRule, rng);
			DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
		}
	}

	[ClientRpc]
	private void RpcStartActiveSoundLoop()
	{
		if ((bool)gauntletActiveLoop)
		{
			gauntletActiveLoopPtr = LoopSoundManager.PlaySoundLoopLocal(RoR2Application.instance.gameObject, gauntletActiveLoop);
		}
	}

	[ClientRpc]
	private void RpcActivateGate(string gateName)
	{
		if (!string.IsNullOrEmpty(gateName))
		{
			SceneInfo.instance.SetGateState(gateName, gateEnabled: true);
		}
	}

	[ClientRpc]
	private void RpcActivateDonut(int donutIndex)
	{
		if (donutIndex < followingDonuts.Length)
		{
			followingDonuts[donutIndex]?.root.SetActive(value: true);
		}
	}

	[ClientRpc]
	private void RpcTryShuffleData(ulong seed)
	{
		TryShuffleData(seed);
	}

	private void StopActiveSoundLoop()
	{
		if ((bool)gauntletActiveLoop)
		{
			LoopSoundManager.StopSoundLoopLocal(gauntletActiveLoopPtr);
		}
	}

	private void SetSkyboxToGauntlet()
	{
		if ((bool)donutSkyboxObject)
		{
			donutSkyboxObject.SetActive(value: false);
		}
		if ((bool)gauntletSkyboxObject)
		{
			gauntletSkyboxObject.SetActive(value: true);
		}
	}

	private void SetSkyboxToDonut()
	{
		if ((bool)donutSkyboxObject)
		{
			donutSkyboxObject.SetActive(value: true);
		}
		if ((bool)gauntletSkyboxObject)
		{
			gauntletSkyboxObject.SetActive(value: false);
		}
	}

	public void OnAuthorityPlayerEnter()
	{
		SetSkyboxToGauntlet();
	}

	public void OnAuthorityPlayerExit()
	{
		SetSkyboxToDonut();
		StopActiveSoundLoop();
	}

	public void PointZoneToGauntlet(int destinationGauntletIndex, MapZone zone)
	{
		if (destinationGauntletIndex < gauntlets.Length)
		{
			GauntletInfo gauntletInfo = gauntlets[destinationGauntletIndex];
			zone.explicitDestination = gauntletInfo.startPoint;
			zone.destinationIdealRadius = gauntletInfo.startRadius;
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcStartActiveSoundLoop(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcStartActiveSoundLoop called on server.");
		}
		else
		{
			((VoidRaidGauntletController)obj).RpcStartActiveSoundLoop();
		}
	}

	protected static void InvokeRpcRpcActivateGate(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcActivateGate called on server.");
		}
		else
		{
			((VoidRaidGauntletController)obj).RpcActivateGate(reader.ReadString());
		}
	}

	protected static void InvokeRpcRpcActivateDonut(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcActivateDonut called on server.");
		}
		else
		{
			((VoidRaidGauntletController)obj).RpcActivateDonut((int)reader.ReadPackedUInt32());
		}
	}

	protected static void InvokeRpcRpcTryShuffleData(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcTryShuffleData called on server.");
		}
		else
		{
			((VoidRaidGauntletController)obj).RpcTryShuffleData(reader.ReadPackedUInt64());
		}
	}

	public void CallRpcStartActiveSoundLoop()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcStartActiveSoundLoop called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcStartActiveSoundLoop);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcStartActiveSoundLoop");
	}

	public void CallRpcActivateGate(string gateName)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcActivateGate called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcActivateGate);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(gateName);
		SendRPCInternal(networkWriter, 0, "RpcActivateGate");
	}

	public void CallRpcActivateDonut(int donutIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcActivateDonut called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcActivateDonut);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32((uint)donutIndex);
		SendRPCInternal(networkWriter, 0, "RpcActivateDonut");
	}

	public void CallRpcTryShuffleData(ulong seed)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcTryShuffleData called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcTryShuffleData);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt64(seed);
		SendRPCInternal(networkWriter, 0, "RpcTryShuffleData");
	}

	static VoidRaidGauntletController()
	{
		kRpcRpcStartActiveSoundLoop = -755317963;
		NetworkBehaviour.RegisterRpcDelegate(typeof(VoidRaidGauntletController), kRpcRpcStartActiveSoundLoop, InvokeRpcRpcStartActiveSoundLoop);
		kRpcRpcActivateGate = -1148984728;
		NetworkBehaviour.RegisterRpcDelegate(typeof(VoidRaidGauntletController), kRpcRpcActivateGate, InvokeRpcRpcActivateGate);
		kRpcRpcActivateDonut = -1261146843;
		NetworkBehaviour.RegisterRpcDelegate(typeof(VoidRaidGauntletController), kRpcRpcActivateDonut, InvokeRpcRpcActivateDonut);
		kRpcRpcTryShuffleData = 20528402;
		NetworkBehaviour.RegisterRpcDelegate(typeof(VoidRaidGauntletController), kRpcRpcTryShuffleData, InvokeRpcRpcTryShuffleData);
		NetworkCRC.RegisterBehaviour("VoidRaidGauntletController", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.WritePackedUInt64(rngSeed);
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
			writer.WritePackedUInt64(rngSeed);
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
			rngSeed = reader.ReadPackedUInt64();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			TryShuffleData(reader.ReadPackedUInt64());
		}
	}

	public override void PreStartClient()
	{
	}
}

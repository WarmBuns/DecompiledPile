using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AK.Wwise;
using EntityStates;
using EntityStates.FalseSonBoss;
using RoR2.CharacterSpeech;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2;

[RequireComponent(typeof(EntityStateMachine))]
public class MeridianEventTriggerInteraction : NetworkBehaviour, IDisplayNameProvider
{
	public enum RingFallingState
	{
		None,
		OuterRingFalling,
		MiddleRingFalling
	}

	public class MeridianEventBaseState : EntityState
	{
		protected MeridianEventTriggerInteraction meridianEventTriggerInteraction => instance;
	}

	public class MeridianEventStart : MeridianEventBaseState
	{
		public static event Action OnMeridianEventStart;

		public override void OnEnter()
		{
			base.OnEnter();
			MeridianEventStart.OnMeridianEventStart?.Invoke();
			base.meridianEventTriggerInteraction.EventPPVolume.SetActive(value: true);
			base.meridianEventTriggerInteraction.HandleNodeGateActivation(0);
			if (NetworkServer.active)
			{
				base.meridianEventTriggerInteraction.CallRpcSetEventMaterials();
			}
			base.meridianEventTriggerInteraction.SetEventObjects();
			base.meridianEventTriggerInteraction.isHeadRotating = true;
			outer.SetNextState(new Phase1());
		}
	}

	public class FSBFEncounterBaseState : EntityState
	{
		protected ChildLocator childLocator;

		protected MeridianEventTriggerInteraction meridianEventTriggerInteraction => instance;

		public override void OnEnter()
		{
			base.OnEnter();
			childLocator = GetComponent<ChildLocator>();
		}

		public override void OnExit()
		{
			base.OnExit();
		}

		public void KillAllMonsters()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			foreach (TeamComponent item in new List<TeamComponent>(TeamComponent.GetTeamMembers(TeamIndex.Monster)))
			{
				if ((bool)item)
				{
					HealthComponent component = item.GetComponent<HealthComponent>();
					if ((bool)component)
					{
						component.Suicide();
					}
				}
			}
		}
	}

	public abstract class FSBFPhaseBaseState : FSBFEncounterBaseState
	{
		[NonSerialized]
		public static bool readyToSpawnNextBossBody;

		[SerializeField]
		public float durationBeforeEnablingCombatEncounter;

		[SerializeField]
		public float durationBeforeRingsSpawn;

		[SerializeField]
		public GameObject speechControllerPrefab;

		[SerializeField]
		public GameObject spawnVFX;

		protected ScriptedCombatEncounter phaseScriptedCombatEncounter;

		protected GameObject phaseControllerObject;

		protected GameObject phaseControllerSubObjectContainer;

		protected BossGroup phaseBossGroup;

		private bool hasSpawned;

		private bool spawnedNextState;

		private const float minimumDurationPerPhase = 2f;

		private Run.FixedTimeStamp healthBarShowTime = Run.FixedTimeStamp.positiveInfinity;

		protected abstract EntityState nextState { get; }

		protected abstract string phaseControllerChildString { get; }

		protected virtual float healthBarShowDelay => 0f;

		public override void OnEnter()
		{
			base.OnEnter();
			if (!base.meridianEventTriggerInteraction.isFirstPhase)
			{
				speechControllerPrefab = base.meridianEventTriggerInteraction.falseSonBoss1PhaseSpeechController;
				base.meridianEventTriggerInteraction.isFirstPhase = true;
			}
			else if (!base.meridianEventTriggerInteraction.isFinalPhase)
			{
				speechControllerPrefab = base.meridianEventTriggerInteraction.falseSonBossEarlySpeechController;
			}
			else
			{
				speechControllerPrefab = base.meridianEventTriggerInteraction.falseSonBossLateSpeechController;
			}
			if ((bool)childLocator)
			{
				phaseControllerObject = childLocator.FindChild(phaseControllerChildString).gameObject;
				if ((bool)phaseControllerObject)
				{
					phaseScriptedCombatEncounter = phaseControllerObject.GetComponent<ScriptedCombatEncounter>();
					phaseBossGroup = phaseControllerObject.GetComponent<BossGroup>();
				}
			}
			healthBarShowTime = Run.FixedTimeStamp.now + healthBarShowDelay;
			if ((bool)DirectorCore.instance)
			{
				CombatDirector[] components = DirectorCore.instance.GetComponents<CombatDirector>();
				for (int i = 0; i < components.Length; i++)
				{
					components[i].enabled = false;
				}
			}
			if (NetworkServer.active && (object)phaseScriptedCombatEncounter != null)
			{
				phaseScriptedCombatEncounter.combatSquad.onMemberAddedServer += OnMemberAddedServer;
			}
		}

		public override void OnExit()
		{
			if (NetworkServer.active)
			{
				if ((object)phaseScriptedCombatEncounter != null)
				{
					phaseScriptedCombatEncounter.combatSquad.onMemberAddedServer -= OnMemberAddedServer;
				}
				if ((bool)phaseControllerSubObjectContainer)
				{
					phaseControllerSubObjectContainer.SetActive(value: false);
				}
			}
			if ((bool)phaseBossGroup)
			{
				phaseBossGroup.shouldDisplayHealthBarOnHud = false;
			}
			base.OnExit();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			phaseBossGroup.shouldDisplayHealthBarOnHud = healthBarShowTime.hasPassed;
			if (!hasSpawned && readyToSpawnNextBossBody && base.fixedAge > durationBeforeEnablingCombatEncounter)
			{
				BeginEncounter();
			}
			else if (hasSpawned && readyToSpawnNextBossBody && !spawnedNextState && base.fixedAge > 2f + durationBeforeEnablingCombatEncounter && (bool)phaseScriptedCombatEncounter && phaseScriptedCombatEncounter.combatSquad.memberCount == 0)
			{
				spawnedNextState = true;
				outer.SetNextState(nextState);
			}
		}

		protected void BeginEncounter()
		{
			readyToSpawnNextBossBody = false;
			hasSpawned = true;
			PreEncounterBegin();
			if (NetworkServer.active)
			{
				phaseScriptedCombatEncounter.BeginEncounter();
			}
		}

		protected virtual void PreEncounterBegin()
		{
		}

		protected virtual void OnMemberAddedServer(CharacterMaster master)
		{
			if ((bool)speechControllerPrefab)
			{
				UnityEngine.Object.Instantiate(speechControllerPrefab, master.transform).GetComponent<CharacterSpeechController>().characterMaster = master;
			}
		}
	}

	public class Phase1 : FSBFPhaseBaseState
	{
		private bool spawnedEntryFX;

		private bool lightningStarted;

		protected override string phaseControllerChildString => "Phase1";

		protected override EntityState nextState => new Phase2();

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (base.fixedAge > 15f && !lightningStarted)
			{
				lightningStarted = true;
				base.meridianEventTriggerInteraction.arenaGeode3.SetActive(value: true);
				base.meridianEventTriggerInteraction.arenaGeode4.SetActive(value: true);
			}
			if (!spawnedEntryFX && base.fixedAge > base.meridianEventTriggerInteraction.additionalEntryVFXDelay + durationBeforeEnablingCombatEncounter)
			{
				spawnedEntryFX = true;
				SetPlayersInvulnerable(b: false);
				EffectManager.SpawnEffect(base.meridianEventTriggerInteraction.falseSonEntryFX, new EffectData
				{
					origin = base.meridianEventTriggerInteraction.falseSonEntryFXPosition.position
				}, transmit: false);
				base.meridianEventTriggerInteraction.musicPhaseNone.SetActive(value: false);
				base.meridianEventTriggerInteraction.musicPhaseOne.SetActive(value: true);
			}
			if (spawnedEntryFX && base.meridianEventTriggerInteraction.meridianColumnsGameObject.activeInHierarchy && !base.meridianEventTriggerInteraction.meridianPlayableColumns.activeInHierarchy && base.fixedAge > base.meridianEventTriggerInteraction.additionalEntryVFXDelay + durationBeforeEnablingCombatEncounter + 3f)
			{
				base.meridianEventTriggerInteraction.meridianColumnsGameObject.SetActive(value: false);
				base.meridianEventTriggerInteraction.meridianPlayableColumns.SetActive(value: true);
			}
		}

		public override void OnEnter()
		{
			base.meridianEventTriggerInteraction.onFalseSonAppearance.SetActive(value: true);
			base.meridianEventTriggerInteraction.HandleNodeGateActivation(0);
			durationBeforeEnablingCombatEncounter = 7f;
			KillAllMonsters();
			base.OnEnter();
			FSBFPhaseBaseState.readyToSpawnNextBossBody = true;
			Util.PlaySound("Play_boss_falseson_VO_spawn", base.gameObject);
			base.meridianEventTriggerInteraction.Boundary1.SetActive(value: true);
			if (NetworkServer.active)
			{
				SetPlayersInvulnerable(b: true);
			}
		}

		protected override void OnMemberAddedServer(CharacterMaster master)
		{
			base.OnMemberAddedServer(master);
		}
	}

	public class Phase2 : FSBFPhaseBaseState
	{
		private bool startRingsFalling;

		protected override string phaseControllerChildString => "Phase2";

		protected override EntityState nextState => new Phase3();

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (base.fixedAge > durationBeforeRingsSpawn && !startRingsFalling)
			{
				base.meridianEventTriggerInteraction.Boundary1.SetActive(value: false);
				base.meridianEventTriggerInteraction.Boundary2.SetActive(value: true);
				base.meridianEventTriggerInteraction.StartOuterRingFalling();
				base.meridianEventTriggerInteraction.arenaGeode3.SetActive(value: false);
				startRingsFalling = true;
			}
		}

		public override void OnEnter()
		{
			base.meridianEventTriggerInteraction.onEnteringPhase2.SetActive(value: true);
			base.meridianEventTriggerInteraction.HandleNodeGateActivation(1);
			base.meridianEventTriggerInteraction.phase2CombatDirector.SetActive(value: true);
			durationBeforeEnablingCombatEncounter = 1f;
			durationBeforeRingsSpawn = 1.75f;
			childLocator = GetComponent<ChildLocator>();
			phaseControllerObject = childLocator.FindChild(phaseControllerChildString).gameObject;
			Util.PlaySound("Play_boss_falseson_VO_spawn", base.gameObject);
			Util.PlaySound("Play_boss_falseson_VO_anger", base.gameObject);
			base.OnEnter();
			SetPlayersInvulnerable(b: false);
		}

		public override void OnExit()
		{
			base.meridianEventTriggerInteraction.isFinalPhase = true;
			base.meridianEventTriggerInteraction.phase2CombatDirector.SetActive(value: false);
			KillAllMonsters();
			base.OnExit();
		}

		protected override void OnMemberAddedServer(CharacterMaster master)
		{
			base.OnMemberAddedServer(master);
		}
	}

	public class Phase3 : FSBFPhaseBaseState
	{
		private bool startRingsFalling;

		protected override string phaseControllerChildString => "Phase3";

		protected override EntityState nextState => new MeridianEventCleared();

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (base.fixedAge > durationBeforeRingsSpawn && !startRingsFalling)
			{
				base.meridianEventTriggerInteraction.Boundary2.SetActive(value: false);
				base.meridianEventTriggerInteraction.Boundary3.SetActive(value: true);
				base.meridianEventTriggerInteraction.StartMiddleRingFalling();
				base.meridianEventTriggerInteraction.arenaGeode4.SetActive(value: false);
				startRingsFalling = true;
			}
		}

		public override void OnEnter()
		{
			speechControllerPrefab = base.meridianEventTriggerInteraction.falseSonBossLateSpeechController;
			if (NetworkServer.active)
			{
				LightningStormController.SetStormStrikePattern(base.meridianEventTriggerInteraction.bossLightningStrikePattern);
				LightningStormController.SetStormActive(b: true);
			}
			base.meridianEventTriggerInteraction.onEnteringPhase3.SetActive(value: true);
			base.meridianEventTriggerInteraction.HandleNodeGateActivation(2);
			durationBeforeEnablingCombatEncounter = 1f;
			durationBeforeRingsSpawn = 1.75f;
			childLocator = GetComponent<ChildLocator>();
			phaseControllerObject = childLocator.FindChild(phaseControllerChildString).gameObject;
			GoldTitanManager.onGoldTitanSpawned += IsAurelionitePresent;
			Util.PlaySound("Play_boss_falseson_VO_spawn", base.gameObject);
			base.OnEnter();
		}

		public override void OnExit()
		{
			base.OnExit();
			GoldTitanManager.onGoldTitanSpawned -= IsAurelionitePresent;
		}

		protected override void OnMemberAddedServer(CharacterMaster master)
		{
			base.OnMemberAddedServer(master);
		}

		private void IsAurelionitePresent()
		{
			instance.isGoldTitanSpawned = true;
		}
	}

	public class MeridianEventCleared : MeridianEventBaseState
	{
		public override void OnEnter()
		{
			base.OnEnter();
			base.meridianEventTriggerInteraction.EventCompletePPVolume.SetActive(value: true);
			base.meridianEventTriggerInteraction.EventPPVolume.SetActive(value: false);
			base.meridianEventTriggerInteraction.shrineModel.SetActive(value: true);
			base.meridianEventTriggerInteraction.shrine.GetComponent<ShrineRebirthController>().stateMachine.SetNextStateToMain();
			base.meridianEventTriggerInteraction.finalPillars.SetActive(value: true);
			base.meridianEventTriggerInteraction.meridianRain.SetActive(value: false);
			base.meridianEventTriggerInteraction.meridianStormCloud.SetActive(value: false);
			base.meridianEventTriggerInteraction.eventClearedVFX.SetActive(value: true);
			base.meridianEventTriggerInteraction.arenaGeode1.SetActive(value: false);
			base.meridianEventTriggerInteraction.arenaGeode2.SetActive(value: false);
			base.meridianEventTriggerInteraction.bossFightAMB.Stop(base.gameObject);
			base.meridianEventTriggerInteraction.bossDefeatAMB.Post(base.gameObject);
			Util.PlaySound("Play_env_meridian_level_cleared", base.gameObject);
			if (!NetworkServer.active)
			{
				return;
			}
			GameObject[] destinationPortals = base.meridianEventTriggerInteraction.destinationPortals;
			for (int i = 0; i < destinationPortals.Length; i++)
			{
				destinationPortals[i].SetActive(value: true);
			}
			foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
			{
				if (!(instance == null))
				{
					CharacterBody body = instance.master.GetBody();
					if (!(body == null))
					{
						Util.CleanseBody(body, removeDebuffs: true, removeBuffs: false, removeCooldownBuffs: false, removeDots: false, removeStun: false, removeNearbyProjectiles: false);
					}
				}
			}
		}
	}

	[HideInInspector]
	public static Transform bossTransform;

	public string contextToken = "MERIDIAN_EVENTTRIGGER_CONTEXT";

	public string displayNameToken = "MERIDIAN_EVENTTRIGGER_NAME";

	public int minimumNumberToSpawnPerMonsterType;

	public int maximumNumberToSpawnBeforeSkipping;

	public GameObject eventGateStateObject;

	public GameObject eventGateStatePhase2Object;

	public GameObject eventGateStatePhase3Object;

	public CombatDirector combatDirectorWave1;

	public CombatDirector combatDirectorWave2;

	public SceneExitController sceneExitController;

	[Header("Post Process Volumes")]
	public GameObject EventPPVolume;

	public GameObject EventCompletePPVolume;

	public GameObject AfterEventCompletePPVolume;

	[Header("Destination Portals")]
	public GameObject[] destinationPortals;

	[Header("Colossus Head")]
	public GameObject colossusHead;

	public Transform colossusHeadFinalPosition;

	public float colossusHeadDuration = 420f;

	private bool isHeadRotating;

	[Header("False Son Core")]
	public GameObject falseSonCore;

	[Header("Game Objects to Modify")]
	public GameObjectsToModify[] gameObjectsToModify;

	[Header("Event Objects to Toggle")]
	public GameObject[] eventGameObjectsToToggle;

	[Header("Cleaered Objects to Toggle")]
	public GameObject[] clearedGameObjectsToToggle;

	[Header("False Son Boss Encounter")]
	public ScriptedCombatEncounter falseSonBossEncounter;

	public BossGroup falseSonBossGroup;

	public CombatDirector combatDirector;

	[Header("Teleport Locs")]
	public GameObject playerTPLoc1;

	public GameObject playerTPLoc2;

	public GameObject playerTPLoc3;

	public GameObject playerTPLoc4;

	private GameObject[] playerTPLocs = new GameObject[4];

	[Header("Cutscene")]
	public GameObject cutsceneCamera;

	public AK.Wwise.Event startSceneAMB;

	public AK.Wwise.Event bossFightAMB;

	public AK.Wwise.Event bossDefeatAMB;

	[Header("Boss Music Phases")]
	public GameObject musicPhaseNone;

	public GameObject musicPhaseOne;

	public GameObject musicPhaseTwo;

	public GameObject musicPhaseThree;

	public GameObject musicPhaseBossDead;

	[Header("Arena Setup")]
	public GameObject lightningStorm;

	public GameObject fog;

	public GameObject sceneBoundsOld;

	public GameObject sceneBoundsNew;

	public GameObject meridianColumnsGameObject;

	public GameObject meridianPlayableColumns;

	public LightningStrikePattern bossLightningStrikePattern;

	[Header("Phase 1 Spawn FX Data")]
	[HideInInspector]
	[SerializeField]
	public GameObject falseSonEntryFX;

	[SerializeField]
	public Transform falseSonEntryFXPosition;

	[SerializeField]
	public float additionalEntryVFXDelay;

	[Header("In-Scene Phase Transition VFX - should automatically disable itself")]
	public GameObject onFalseSonAppearance;

	public GameObject onEnteringPhase2;

	public GameObject onEnteringPhase3;

	[Space(20f)]
	public float ringFallOffTime = 50f;

	[HideInInspector]
	public int ringFallingState;

	private int localRingFallingState;

	[Header("Rings Locs")]
	public GameObject ringPiece1;

	public GameObject ringPiece2;

	public GameObject ringPiece3;

	public GameObject ringPiece4;

	public GameObject ringPiece5;

	public GameObject ringPiece6;

	public GameObject ringPiece7;

	public GameObject ringPiece8;

	public GameObject ringPiece9;

	public GameObject ringPiece10;

	public GameObject ringPiece1FinalPosition;

	public GameObject ringPiece2FinalPosition;

	public GameObject ringPiece3FinalPosition;

	public GameObject ringPiece4FinalPosition;

	public GameObject ringPiece5FinalPosition;

	public GameObject ringPiece6FinalPosition;

	public GameObject ringPiece7FinalPosition;

	public GameObject ringPiece8FinalPosition;

	public GameObject ringPiece9FinalPosition;

	public GameObject ringPiece10FinalPosition;

	[Header("Arena Finale")]
	public GameObject shrine;

	public GameObject shrineModel;

	public GameObject finalPillars;

	public GameObject eventClearedVFX;

	public GameObject meridianRain;

	public GameObject meridianStormCloud;

	[Header("Geode System")]
	public GameObject arenaGeode1;

	public GameObject arenaGeode2;

	public GameObject arenaGeode3;

	public GameObject arenaGeode4;

	[Header("Boss Bounds System")]
	public GameObject Boundary1;

	public GameObject Boundary2;

	public GameObject Boundary3;

	[Header("Boss Dialogue Prefabs")]
	public GameObject falseSonBossEarlySpeechController;

	public GameObject falseSonBossLateSpeechController;

	public GameObject falseSonBoss1PhaseSpeechController;

	[HideInInspector]
	public bool isFirstPhase;

	[HideInInspector]
	public bool isFinalPhase;

	[HideInInspector]
	public bool isGoldTitanSpawned;

	[Header("Monster Waves Combat Director Prefabs")]
	public GameObject phase2CombatDirector;

	private EntityStateMachine mainStateMachine;

	private bool triggerEventClear;

	[SyncVar(hook = "Client_HandleStartMeridianEvent")]
	public bool isTriggered;

	private static int kRpcRpcTeleportWithLocalAuthority;

	private static int kRpcRpcOnFalseSonDefeated;

	private static int kRpcRpcSetEventMaterials;

	public static MeridianEventTriggerInteraction instance { get; private set; }

	protected MeridianEventTriggerInteraction eventTriggerInteraction { get; private set; }

	public bool NetworkisTriggered
	{
		get
		{
			return isTriggered;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				Client_HandleStartMeridianEvent(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref isTriggered, 1u);
		}
	}

	public static event Action<MeridianEventTriggerInteraction> onDefeatedServerGlobal;

	public static event Action<MeridianEventTriggerInteraction, Interactor> onActivated;

	public static event Action onInstanceChangedGlobal;

	public string GetContextString(Interactor activator)
	{
		return Language.GetString(contextToken);
	}

	public override int GetNetworkChannel()
	{
		return QosChannelIndex.defaultReliable.intVal;
	}

	public Interactability GetInteractability(Interactor activator)
	{
		if (isTriggered)
		{
			return Interactability.Disabled;
		}
		return Interactability.Available;
	}

	public string GetDisplayName()
	{
		return Language.GetString(displayNameToken);
	}

	public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
	{
		return false;
	}

	public bool ShouldShowOnScanner()
	{
		return !isTriggered;
	}

	private void Start()
	{
		eventTriggerInteraction = GetComponent<MeridianEventTriggerInteraction>();
	}

	public void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
		MeridianEventTriggerInteraction.onInstanceChangedGlobal?.Invoke();
		if (falseSonEntryFX == null)
		{
			AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("FalseSonBoss/FalseSonBossAppearVFX");
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> result)
			{
				falseSonEntryFX = result.Result;
			};
		}
	}

	public void OnDisable()
	{
		instance = SingletonHelper.Unassign(instance, this);
		MeridianEventTriggerInteraction.onInstanceChangedGlobal?.Invoke();
	}

	[Server]
	public override void OnStartServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.MeridianEventTriggerInteraction::OnStartServer()' called on client");
			return;
		}
		base.OnStartServer();
		startSceneAMB.Post(base.gameObject);
	}

	public void OnTriggerEnter(Collider other)
	{
		if (NetworkServer.active && !isTriggered && other.TryGetComponent<CharacterBody>(out var component) && component.isPlayerControlled)
		{
			HandleInitializeMeridianEvent();
		}
	}

	public static void SetPlayersInvulnerable(bool b)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
		{
			if (instance == null)
			{
				continue;
			}
			CharacterBody body = instance.master.GetBody();
			if (!(body == null))
			{
				if (b)
				{
					body.AddTimedBuff(DLC2Content.Buffs.HiddenRejectAllDamage, 1.5f);
				}
				else if (body.HasBuff(DLC2Content.Buffs.HiddenRejectAllDamage))
				{
					body.RemoveBuff(DLC2Content.Buffs.HiddenRejectAllDamage);
				}
			}
		}
	}

	[Server]
	private void HandleInitializeMeridianEvent()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.MeridianEventTriggerInteraction::HandleInitializeMeridianEvent()' called on client");
			return;
		}
		LightningStormController.SetStormActive(b: false);
		combatDirector.enabled = false;
		playerTPLocs[0] = playerTPLoc1;
		playerTPLocs[1] = playerTPLoc2;
		playerTPLocs[2] = playerTPLoc3;
		playerTPLocs[3] = playerTPLoc4;
		int num = 0;
		int num2 = 0;
		int count = PlayerCharacterMasterController.instances.Count;
		bool flag = count > 4;
		foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
		{
			if (instance == null)
			{
				continue;
			}
			Vector3 vector;
			Quaternion rotation;
			if (flag)
			{
				int num3 = playerTPLocs.Length - 1;
				float num4 = (float)num2 / (float)(count - 1) * (float)num3;
				int num5 = Mathf.Clamp(Mathf.FloorToInt(num4), 0, num3 - 1);
				float t = num4 - (float)num5;
				Transform obj = playerTPLocs[num5].transform;
				Transform transform = playerTPLocs[num5 + 1].transform;
				vector = Vector3.Lerp(obj.position, transform.position, t);
				rotation = Quaternion.Lerp(obj.rotation, transform.rotation, t);
			}
			else
			{
				int num6 = num2;
				if (playerTPLocs[num6] == null)
				{
					num6 = num;
				}
				Transform obj2 = playerTPLocs[num6].transform;
				vector = obj2.position;
				rotation = obj2.rotation;
			}
			CharacterBody body = instance.master.GetBody();
			if (!(body == null))
			{
				body.currentVehicle?.EjectPassenger();
				if (Util.HasEffectiveAuthority(body.gameObject))
				{
					TeleportHelper.TeleportBody(body, vector);
				}
				else
				{
					CallRpcTeleportWithLocalAuthority(body.gameObject, vector, rotation);
				}
				num2++;
			}
		}
		NetworkisTriggered = true;
	}

	[ClientRpc]
	private void RpcTeleportWithLocalAuthority(GameObject playerObject, Vector3 position, Quaternion rotation)
	{
		if (Util.HasEffectiveAuthority(playerObject))
		{
			CharacterBody component = playerObject.GetComponent<CharacterBody>();
			if (component == null)
			{
				Debug.LogWarning("Received RPCTeleport request but the game object doesn't have a CharacterBody! " + playerObject.name, playerObject);
			}
			else
			{
				TeleportHelper.TeleportBody(component, position);
			}
		}
	}

	[Client]
	private void Client_HandleStartMeridianEvent(bool newValue)
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void RoR2.MeridianEventTriggerInteraction::Client_HandleStartMeridianEvent(System.Boolean)' called on server");
			return;
		}
		NetworkisTriggered = newValue;
		HandleStartMeridianEvent();
	}

	private void HandleStartMeridianEvent()
	{
		fog.SetActive(value: true);
		sceneBoundsOld.SetActive(value: false);
		sceneBoundsNew.SetActive(value: true);
		startSceneAMB.Stop(base.gameObject);
		bossFightAMB.Post(base.gameObject);
		musicPhaseNone.SetActive(value: true);
		cutsceneCamera.SetActive(value: true);
		Util.PlaySound("Play_env_meridian_activate_boss_cutscene", base.gameObject);
		LocalUser firstLocalUser = LocalUserManager.GetFirstLocalUser();
		if (firstLocalUser != null && firstLocalUser.cachedBody != null)
		{
			string bodyName = BodyCatalog.GetBodyName(firstLocalUser.cachedBody.bodyIndex);
			firstLocalUser.userProfile.HasAchievement(bodyName.Substring(0, bodyName.Length - 4) + "ClearMeridianEvent");
		}
		mainStateMachine.SetNextState(new MeridianEventStart());
	}

	private IEnumerator RotateHead()
	{
		float timeElapsed = 0f;
		while (timeElapsed < colossusHeadDuration)
		{
			colossusHead.transform.rotation = Quaternion.Lerp(colossusHead.transform.rotation, colossusHeadFinalPosition.rotation, timeElapsed / colossusHeadDuration);
			ringPiece1.transform.rotation = Quaternion.Lerp(ringPiece1.transform.rotation, ringPiece1FinalPosition.transform.rotation, timeElapsed / (colossusHeadDuration - 20f));
			ringPiece2.transform.rotation = Quaternion.Lerp(ringPiece2.transform.rotation, ringPiece2FinalPosition.transform.rotation, timeElapsed / (colossusHeadDuration - 22f));
			ringPiece3.transform.rotation = Quaternion.Lerp(ringPiece3.transform.rotation, ringPiece3FinalPosition.transform.rotation, timeElapsed / (colossusHeadDuration - 25f));
			ringPiece4.transform.rotation = Quaternion.Lerp(ringPiece4.transform.rotation, ringPiece4FinalPosition.transform.rotation, timeElapsed / (colossusHeadDuration - 26f));
			ringPiece5.transform.rotation = Quaternion.Lerp(ringPiece5.transform.rotation, ringPiece5FinalPosition.transform.rotation, timeElapsed / (colossusHeadDuration - 19f));
			ringPiece1.transform.position = Vector3.Lerp(ringPiece1.transform.position, ringPiece1FinalPosition.transform.position, timeElapsed / (colossusHeadDuration - 20f));
			ringPiece2.transform.position = Vector3.Lerp(ringPiece2.transform.position, ringPiece2FinalPosition.transform.position, timeElapsed / (colossusHeadDuration - 22f));
			ringPiece3.transform.position = Vector3.Lerp(ringPiece3.transform.position, ringPiece3FinalPosition.transform.position, timeElapsed / (colossusHeadDuration - 25f));
			ringPiece4.transform.position = Vector3.Lerp(ringPiece4.transform.position, ringPiece4FinalPosition.transform.position, timeElapsed / (colossusHeadDuration - 26f));
			ringPiece5.transform.position = Vector3.Lerp(ringPiece5.transform.position, ringPiece5FinalPosition.transform.position, timeElapsed / (colossusHeadDuration - 19f));
			ringPiece6.transform.rotation = Quaternion.Lerp(ringPiece6.transform.rotation, ringPiece6FinalPosition.transform.rotation, timeElapsed / colossusHeadDuration);
			ringPiece7.transform.rotation = Quaternion.Lerp(ringPiece7.transform.rotation, ringPiece7FinalPosition.transform.rotation, timeElapsed / colossusHeadDuration);
			ringPiece8.transform.rotation = Quaternion.Lerp(ringPiece8.transform.rotation, ringPiece8FinalPosition.transform.rotation, timeElapsed / colossusHeadDuration);
			ringPiece9.transform.rotation = Quaternion.Lerp(ringPiece9.transform.rotation, ringPiece9FinalPosition.transform.rotation, timeElapsed / colossusHeadDuration);
			ringPiece10.transform.rotation = Quaternion.Lerp(ringPiece10.transform.rotation, ringPiece10FinalPosition.transform.rotation, timeElapsed / colossusHeadDuration);
			ringPiece6.transform.position = Vector3.Lerp(ringPiece6.transform.position, ringPiece6FinalPosition.transform.position, timeElapsed / colossusHeadDuration);
			ringPiece7.transform.position = Vector3.Lerp(ringPiece7.transform.position, ringPiece7FinalPosition.transform.position, timeElapsed / colossusHeadDuration);
			ringPiece8.transform.position = Vector3.Lerp(ringPiece8.transform.position, ringPiece8FinalPosition.transform.position, timeElapsed / colossusHeadDuration);
			ringPiece9.transform.position = Vector3.Lerp(ringPiece9.transform.position, ringPiece9FinalPosition.transform.position, timeElapsed / colossusHeadDuration);
			ringPiece10.transform.position = Vector3.Lerp(ringPiece10.transform.position, ringPiece10FinalPosition.transform.position, timeElapsed / colossusHeadDuration);
			timeElapsed += Time.deltaTime;
			yield return null;
		}
	}

	private void StartOuterRingFalling()
	{
		ringFallingState = Mathf.Max(1, ringFallingState);
		Util.PlaySound("Play_env_meridian_level_ring_fall", base.gameObject);
	}

	private void StartMiddleRingFalling()
	{
		ringFallingState = Mathf.Max(2, ringFallingState);
		Util.PlaySound("Play_env_meridian_level_ring_fall", base.gameObject);
	}

	private IEnumerator OuterRingFalling()
	{
		float timeElapsed = 0f;
		while (timeElapsed < ringFallOffTime)
		{
			ringPiece6.transform.position = Vector3.Lerp(ringPiece6.transform.position, ringPiece6.transform.position - new Vector3(0f, 100f, 0f), timeElapsed / ringFallOffTime);
			ringPiece7.transform.position = Vector3.Lerp(ringPiece7.transform.position, ringPiece7.transform.position - new Vector3(0f, 230f, 0f), timeElapsed / ringFallOffTime);
			ringPiece8.transform.position = Vector3.Lerp(ringPiece8.transform.position, ringPiece8.transform.position - new Vector3(0f, 120f, 0f), timeElapsed / ringFallOffTime);
			ringPiece9.transform.position = Vector3.Lerp(ringPiece9.transform.position, ringPiece9.transform.position - new Vector3(0f, 350f, 0f), timeElapsed / ringFallOffTime);
			ringPiece10.transform.position = Vector3.Lerp(ringPiece10.transform.position, ringPiece10.transform.position - new Vector3(0f, 130f, 0f), timeElapsed / ringFallOffTime);
			timeElapsed += Time.deltaTime;
			yield return null;
		}
		ringPiece6.SetActive(value: false);
		ringPiece7.SetActive(value: false);
		ringPiece8.SetActive(value: false);
		ringPiece9.SetActive(value: false);
		ringPiece10.SetActive(value: false);
	}

	private IEnumerator MiddleRingFalling()
	{
		float timeElapsed = 0f;
		while (timeElapsed < ringFallOffTime)
		{
			ringPiece1.transform.position = Vector3.Lerp(ringPiece1.transform.position, ringPiece1.transform.position - new Vector3(0f, 120f, 0f), timeElapsed / ringFallOffTime);
			ringPiece2.transform.position = Vector3.Lerp(ringPiece2.transform.position, ringPiece2.transform.position - new Vector3(0f, 110f, 0f), timeElapsed / ringFallOffTime);
			ringPiece3.transform.position = Vector3.Lerp(ringPiece3.transform.position, ringPiece3.transform.position - new Vector3(0f, 350f, 0f), timeElapsed / ringFallOffTime);
			ringPiece4.transform.position = Vector3.Lerp(ringPiece4.transform.position, ringPiece4.transform.position - new Vector3(0f, 130f, 0f), timeElapsed / ringFallOffTime);
			ringPiece5.transform.position = Vector3.Lerp(ringPiece5.transform.position, ringPiece5.transform.position - new Vector3(0f, 470f, 0f), timeElapsed / ringFallOffTime);
			timeElapsed += Time.deltaTime;
			yield return null;
		}
		ringPiece1.SetActive(value: false);
		ringPiece2.SetActive(value: false);
		ringPiece3.SetActive(value: false);
		ringPiece4.SetActive(value: false);
		ringPiece5.SetActive(value: false);
	}

	public void TriggerEventClear()
	{
		triggerEventClear = true;
	}

	public void Update()
	{
		if (isHeadRotating)
		{
			StartCoroutine(RotateHead());
			cutsceneCamera.SetActive(value: true);
			isHeadRotating = false;
		}
		if (ringFallingState > localRingFallingState)
		{
			if (ringFallingState == 1)
			{
				StartCoroutine(OuterRingFalling());
			}
			else if (ringFallingState == 2)
			{
				StartCoroutine(MiddleRingFalling());
			}
			localRingFallingState = ringFallingState;
		}
		if (triggerEventClear && NetworkServer.active)
		{
			CallRpcOnFalseSonDefeated();
			triggerEventClear = false;
		}
	}

	private void Awake()
	{
		mainStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "Main");
		if (NetworkServer.active)
		{
			SkyJumpDeathState.falseSonDeathEvent += TriggerEventClear;
		}
		GameObject[] array = destinationPortals;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(value: false);
		}
	}

	[ClientRpc]
	private void RpcOnFalseSonDefeated()
	{
		EventCompletePPVolume.SetActive(value: true);
		EventPPVolume.SetActive(value: false);
		if (AfterEventCompletePPVolume != null)
		{
			AfterEventCompletePPVolume.SetActive(value: true);
		}
		SetIdleMaterials();
		SetClearedObjects();
	}

	private void Wave1OnDefeatedServer()
	{
		combatDirectorWave2.enabled = true;
		combatDirectorWave2.currentSpawnTarget = base.gameObject;
		combatDirectorWave2.monsterCredit += (int)(600f * Mathf.Pow(Run.instance.compensatedDifficultyCoefficient, 0.5f));
	}

	private void Wave2OnDefeatedServer()
	{
		mainStateMachine.SetNextState(new MeridianEventCleared());
	}

	private void SetEventObjects()
	{
		for (int i = 0; i < eventGameObjectsToToggle.Length; i++)
		{
			if (!(eventGameObjectsToToggle[i] == null))
			{
				bool activeInHierarchy = eventGameObjectsToToggle[i].activeInHierarchy;
				eventGameObjectsToToggle[i].SetActive(!activeInHierarchy);
			}
		}
	}

	private void HandleNodeGateActivation(int currentState)
	{
		eventGateStateObject.SetActive(currentState <= 0);
		eventGateStatePhase2Object.SetActive(currentState <= 1);
		eventGateStatePhase3Object.SetActive(currentState <= 2);
	}

	private void SetClearedObjects()
	{
		for (int i = 0; i < clearedGameObjectsToToggle.Length; i++)
		{
			if (!(clearedGameObjectsToToggle[i] == null))
			{
				bool activeInHierarchy = clearedGameObjectsToToggle[i].activeInHierarchy;
				clearedGameObjectsToToggle[i].SetActive(!activeInHierarchy);
			}
		}
	}

	[ClientRpc]
	private void RpcSetEventMaterials()
	{
		for (int i = 0; i < gameObjectsToModify.Length; i++)
		{
			GameObject[] objects = gameObjectsToModify[i].objects;
			foreach (GameObject gameObject in objects)
			{
				if (!(gameObject == null) && gameObject.TryGetComponent<Renderer>(out var component))
				{
					component.material = gameObjectsToModify[i].eventMaterial;
				}
			}
		}
	}

	private void SetIdleMaterials()
	{
		for (int i = 0; i < gameObjectsToModify.Length; i++)
		{
			GameObject[] objects = gameObjectsToModify[i].objects;
			foreach (GameObject gameObject in objects)
			{
				if (!(gameObject == null))
				{
					gameObject.GetComponent<Renderer>().material = gameObjectsToModify[i].idleMaterial;
				}
			}
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcTeleportWithLocalAuthority(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcTeleportWithLocalAuthority called on server.");
		}
		else
		{
			((MeridianEventTriggerInteraction)obj).RpcTeleportWithLocalAuthority(reader.ReadGameObject(), reader.ReadVector3(), reader.ReadQuaternion());
		}
	}

	protected static void InvokeRpcRpcOnFalseSonDefeated(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcOnFalseSonDefeated called on server.");
		}
		else
		{
			((MeridianEventTriggerInteraction)obj).RpcOnFalseSonDefeated();
		}
	}

	protected static void InvokeRpcRpcSetEventMaterials(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSetEventMaterials called on server.");
		}
		else
		{
			((MeridianEventTriggerInteraction)obj).RpcSetEventMaterials();
		}
	}

	public void CallRpcTeleportWithLocalAuthority(GameObject playerObject, Vector3 position, Quaternion rotation)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcTeleportWithLocalAuthority called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcTeleportWithLocalAuthority);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(playerObject);
		networkWriter.Write(position);
		networkWriter.Write(rotation);
		SendRPCInternal(networkWriter, 0, "RpcTeleportWithLocalAuthority");
	}

	public void CallRpcOnFalseSonDefeated()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcOnFalseSonDefeated called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcOnFalseSonDefeated);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcOnFalseSonDefeated");
	}

	public void CallRpcSetEventMaterials()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcSetEventMaterials called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcSetEventMaterials);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcSetEventMaterials");
	}

	static MeridianEventTriggerInteraction()
	{
		kRpcRpcTeleportWithLocalAuthority = -40849326;
		NetworkBehaviour.RegisterRpcDelegate(typeof(MeridianEventTriggerInteraction), kRpcRpcTeleportWithLocalAuthority, InvokeRpcRpcTeleportWithLocalAuthority);
		kRpcRpcOnFalseSonDefeated = 1048548329;
		NetworkBehaviour.RegisterRpcDelegate(typeof(MeridianEventTriggerInteraction), kRpcRpcOnFalseSonDefeated, InvokeRpcRpcOnFalseSonDefeated);
		kRpcRpcSetEventMaterials = 1585739;
		NetworkBehaviour.RegisterRpcDelegate(typeof(MeridianEventTriggerInteraction), kRpcRpcSetEventMaterials, InvokeRpcRpcSetEventMaterials);
		NetworkCRC.RegisterBehaviour("MeridianEventTriggerInteraction", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(isTriggered);
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
			writer.Write(isTriggered);
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
			isTriggered = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			Client_HandleStartMeridianEvent(reader.ReadBoolean());
		}
	}

	public override void PreStartClient()
	{
	}
}

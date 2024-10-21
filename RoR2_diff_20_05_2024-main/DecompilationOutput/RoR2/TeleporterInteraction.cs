using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using EntityStates;
using RoR2.CharacterAI;
using RoR2.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(HoldoutZoneController))]
[RequireComponent(typeof(SceneExitController))]
public sealed class TeleporterInteraction : NetworkBehaviour, IInteractable
{
	public enum ActivationState
	{
		Idle,
		IdleToCharging,
		Charging,
		Charged,
		Finished
	}

	private abstract class BaseTeleporterState : BaseState
	{
		protected TeleporterInteraction teleporterInteraction { get; private set; }

		public abstract ActivationState backwardsCompatibleActivationState { get; }

		protected virtual bool shouldEnableChargingSphere => false;

		public override void OnEnter()
		{
			base.OnEnter();
			teleporterInteraction = GetComponent<TeleporterInteraction>();
			teleporterInteraction.holdoutZoneController.enabled = shouldEnableChargingSphere;
		}

		public virtual Interactability GetInteractability(Interactor activator)
		{
			return Interactability.Disabled;
		}

		public virtual string GetContextString(Interactor activator)
		{
			return null;
		}

		public virtual void OnInteractionBegin(Interactor activator)
		{
		}

		protected void SetChildActive(string childLocatorName, bool newActive)
		{
			Transform transform = teleporterInteraction.modelChildLocator.FindChild(childLocatorName);
			if ((bool)transform)
			{
				transform.gameObject.SetActive(newActive);
			}
		}
	}

	private class IdleState : BaseTeleporterState
	{
		private float _tutorialWavePillarTimer;

		private bool _canTutorialWavePillarTrigger = TutorialManager.isTutorialEnabled;

		public override ActivationState backwardsCompatibleActivationState => ActivationState.Idle;

		public override Interactability GetInteractability(Interactor activator)
		{
			return Interactability.Available;
		}

		public override string GetContextString(Interactor activator)
		{
			return Language.GetString(base.teleporterInteraction.beginContextString);
		}

		public override void OnInteractionBegin(Interactor activator)
		{
			Chat.SendBroadcastChat(new SubjectChatMessage
			{
				subjectAsCharacterBody = activator.GetComponent<CharacterBody>(),
				baseToken = "PLAYER_ACTIVATED_TELEPORTER"
			});
			if (base.teleporterInteraction.showExtraBossesIndicator)
			{
				Chat.SendBroadcastChat(new Chat.SimpleChatMessage
				{
					baseToken = "SHRINE_BOSS_BEGIN_TRIAL"
				});
			}
			base.teleporterInteraction.chargeActivatorServer = activator.gameObject;
			outer.SetNextState(new IdleToChargingState());
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (!_canTutorialWavePillarTrigger)
			{
				return;
			}
			_tutorialWavePillarTimer -= Time.fixedDeltaTime;
			if (_tutorialWavePillarTimer < 0f)
			{
				_tutorialWavePillarTimer = base.teleporterInteraction.triggerWavePillarInterval;
				if (Run.instance.ambientLevel >= base.teleporterInteraction.sceneStartAmbientLevel + 2f)
				{
					_canTutorialWavePillarTrigger = false;
					SetChildActive("TutorialWavePillar", newActive: true);
				}
			}
		}

		public override void OnExit()
		{
			base.OnExit();
			SetChildActive("TutorialWavePillar", newActive: false);
		}
	}

	private class IdleToChargingState : BaseTeleporterState
	{
		public override ActivationState backwardsCompatibleActivationState => ActivationState.IdleToCharging;

		public override void OnEnter()
		{
			base.OnEnter();
			SetChildActive("IdleToChargingEffect", newActive: true);
			SetChildActive("PPVolume", newActive: true);
			base.teleporterInteraction.NetworkisActivated = true;
			base.teleporterInteraction.teleporterPositionIndicator.GetComponent<ChargeIndicatorController>().isActivated = true;
		}

		public override void OnExit()
		{
			SetChildActive("IdleToChargingEffect", newActive: false);
			base.OnExit();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (base.fixedAge > 3f && NetworkServer.active)
			{
				outer.SetNextState(new ChargingState());
			}
		}
	}

	private class ChargingState : BaseTeleporterState
	{
		private CombatDirector bonusDirector => base.teleporterInteraction.bonusDirector;

		private CombatDirector bossDirector => base.teleporterInteraction.bossDirector;

		protected override bool shouldEnableChargingSphere => true;

		public override ActivationState backwardsCompatibleActivationState => ActivationState.Charging;

		public override void OnEnter()
		{
			base.OnEnter();
			TeleporterInteraction.onTeleporterBeginChargingGlobal?.Invoke(base.teleporterInteraction);
			if (NetworkServer.active)
			{
				if ((bool)bonusDirector)
				{
					bonusDirector.enabled = true;
				}
				if ((bool)bossDirector)
				{
					bossDirector.enabled = true;
					bossDirector.monsterCredit += (int)(600f * Mathf.Pow(Run.instance.compensatedDifficultyCoefficient, 0.5f) * (float)(1 + base.teleporterInteraction.shrineBonusStacks));
					bossDirector.currentSpawnTarget = base.gameObject;
					bossDirector.SetNextSpawnAsBoss();
				}
				if ((bool)DirectorCore.instance)
				{
					CombatDirector[] components = DirectorCore.instance.GetComponents<CombatDirector>();
					if (components.Length != 0)
					{
						CombatDirector[] array = components;
						for (int i = 0; i < array.Length; i++)
						{
							array[i].enabled = false;
						}
					}
				}
				if ((bool)base.teleporterInteraction.outsideInteractableLocker)
				{
					base.teleporterInteraction.outsideInteractableLocker.enabled = true;
				}
				ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(TeamIndex.Player);
				for (int j = 0; j < teamMembers.Count; j++)
				{
					TeamComponent teamComponent = teamMembers[j];
					CharacterBody body = teamComponent.body;
					if (!body)
					{
						continue;
					}
					CharacterMaster master = teamComponent.body.master;
					if ((bool)master)
					{
						int itemCount = master.inventory.GetItemCount(RoR2Content.Items.WardOnLevel);
						if (itemCount > 0)
						{
							GameObject obj = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/WarbannerWard"), body.transform.position, Quaternion.identity);
							obj.GetComponent<TeamFilter>().teamIndex = TeamIndex.Player;
							obj.GetComponent<BuffWard>().Networkradius = 8f + 8f * (float)itemCount;
							NetworkServer.Spawn(obj);
						}
					}
				}
			}
			SetChildActive("ChargingEffect", newActive: true);
		}

		public override void OnExit()
		{
			if (NetworkServer.active)
			{
				if ((bool)base.teleporterInteraction.outsideInteractableLocker)
				{
					base.teleporterInteraction.outsideInteractableLocker.enabled = false;
				}
				if ((bool)bossDirector)
				{
					bossDirector.enabled = false;
				}
				if ((bool)bonusDirector)
				{
					bonusDirector.enabled = false;
				}
			}
			SetChildActive("ChargingEffect", newActive: false);
			base.OnExit();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (NetworkServer.active)
			{
				if (base.teleporterInteraction.holdoutZoneController.charge >= 1f)
				{
					if ((bool)bonusDirector)
					{
						bonusDirector.enabled = false;
					}
					if (base.teleporterInteraction.monstersCleared)
					{
						if ((bool)bossDirector)
						{
							bossDirector.enabled = false;
						}
						outer.SetNextState(new ChargedState());
					}
				}
				if ((bool)base.teleporterInteraction.outsideInteractableLocker)
				{
					base.teleporterInteraction.outsideInteractableLocker.radius = base.teleporterInteraction.holdoutZoneController.currentRadius;
				}
			}
			if ((bool)SceneWeatherController.instance)
			{
				SceneWeatherController.instance.weatherLerp = SceneWeatherController.instance.weatherLerpOverChargeTime.Evaluate((float)base.teleporterInteraction.chargePercent * 0.01f);
			}
			base.teleporterInteraction.UpdateMonstersClear();
		}

		public override Interactability GetInteractability(Interactor activator)
		{
			return Interactability.ConditionsNotMet;
		}
	}

	private class ChargedState : BaseTeleporterState
	{
		public override ActivationState backwardsCompatibleActivationState => ActivationState.Charged;

		public override void OnEnter()
		{
			base.OnEnter();
			base.teleporterInteraction.teleporterPositionIndicator.GetComponent<ChargeIndicatorController>().isCharged = true;
			SetChildActive("ChargedEffect", newActive: true);
			if (NetworkServer.active)
			{
				base.teleporterInteraction.AttemptToSpawnAllEligiblePortals();
				if (RunArtifactManager.instance.IsArtifactEnabled(CU8Content.Artifacts.Delusion))
				{
					ResetChests();
				}
			}
			TeleporterInteraction.onTeleporterChargedGlobal?.Invoke(base.teleporterInteraction);
		}

		private void ResetChests()
		{
			foreach (ChestBehavior instances in InstanceTracker.GetInstancesList<ChestBehavior>())
			{
				instances.CallRpcResetChests();
			}
		}

		public override void OnExit()
		{
			SetChildActive("ChargedEffect", newActive: false);
			base.OnExit();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
		}

		public override Interactability GetInteractability(Interactor activator)
		{
			if (!base.teleporterInteraction.monstersCleared)
			{
				return Interactability.ConditionsNotMet;
			}
			return Interactability.Available;
		}

		public override string GetContextString(Interactor activator)
		{
			return Language.GetString(base.teleporterInteraction.exitContextString);
		}

		public override void OnInteractionBegin(Interactor activator)
		{
			outer.SetNextState(new FinishedState());
		}
	}

	private class FinishedState : BaseTeleporterState
	{
		public override ActivationState backwardsCompatibleActivationState => ActivationState.Finished;

		public override void OnEnter()
		{
			base.OnEnter();
			base.teleporterInteraction.sceneExitController.Begin();
			TeleporterInteraction.onTeleporterFinishGlobal?.Invoke(base.teleporterInteraction);
		}
	}

	[Header("Component and Object References")]
	public CombatDirector bonusDirector;

	public CombatDirector bossDirector;

	public EntityStateMachine mainStateMachine;

	public OutsideInteractableLocker outsideInteractableLocker;

	[Header("Interactability")]
	public string beginContextString;

	public string exitContextString;

	[Header("Teleporter Tutorial")]
	public float triggerWavePillarInterval = 1f;

	private Dictionary<PurchaseInteraction, GameObject> interactableObjectMap;

	private BossGroup bossGroup;

	private ChildLocator modelChildLocator;

	[SyncVar]
	private bool _locked;

	[SyncVar(hook = "OnSyncTeleporterDiscovered")]
	private bool isDiscovered;

	[SyncVar]
	private bool isActivated;

	[SerializeField]
	private float discoveryRadius = 20f;

	private PositionIndicator teleporterPositionIndicator;

	private ChargeIndicatorController teleporterChargeIndicatorController;

	private Color originalTeleporterColor;

	private GameObject bossShrineIndicator;

	[SyncVar(hook = "OnSyncShouldAttemptToSpawnShopPortal")]
	private bool _shouldAttemptToSpawnShopPortal;

	[SyncVar(hook = "OnSyncShouldAttemptToSpawnGoldshoresPortal")]
	private bool _shouldAttemptToSpawnGoldshoresPortal;

	[SyncVar(hook = "OnSyncShouldAttemptToSpawnMSPortal")]
	private bool _shouldAttemptToSpawnMSPortal;

	private Xoroshiro128Plus rng;

	private GameObject chargeActivatorServer;

	private bool monstersCleared;

	[SyncVar]
	private bool showExtraBossesIndicator;

	public SpawnCard shopPortalSpawnCard;

	public SpawnCard goldshoresPortalSpawnCard;

	public SpawnCard msPortalSpawnCard;

	public PortalSpawner[] portalSpawners;

	public float baseShopSpawnChance = 0.375f;

	private List<PingIndicator> currentPings = new List<PingIndicator>();

	private Particle_SetMinSize cachedParticleMinSizeScript;

	private float particleScaleUpdateTimer;

	private const float particleScaleUpdateInterval = 0.5f;

	private bool shouldScaleParticles;

	private LocalUser cachedLocalUser;

	private static int kRpcRpcClientOnActivated;

	public float sceneStartAmbientLevel { get; private set; }

	public HoldoutZoneController holdoutZoneController { get; private set; }

	public SceneExitController sceneExitController { get; private set; }

	private BaseTeleporterState currentState => mainStateMachine.state as BaseTeleporterState;

	public ActivationState activationState => currentState?.backwardsCompatibleActivationState ?? ActivationState.Idle;

	public bool locked
	{
		get
		{
			return _locked;
		}
		set
		{
			Network_locked = value;
		}
	}

	public bool isIdle => currentState is IdleState;

	public bool isIdleToCharging => currentState is IdleToChargingState;

	public bool isInFinalSequence => currentState is FinishedState;

	public bool isCharging => currentState is ChargingState;

	public bool isCharged => activationState >= ActivationState.Charged;

	public float chargeFraction => holdoutZoneController.charge;

	public int shrineBonusStacks { get; set; }

	private int chargePercent => holdoutZoneController.displayChargePercent;

	public static TeleporterInteraction instance { get; private set; }

	public bool shouldAttemptToSpawnShopPortal
	{
		get
		{
			return _shouldAttemptToSpawnShopPortal;
		}
		set
		{
			if (_shouldAttemptToSpawnShopPortal != value)
			{
				Network_shouldAttemptToSpawnShopPortal = value;
				if (_shouldAttemptToSpawnShopPortal && NetworkServer.active)
				{
					Chat.SendBroadcastChat(new Chat.SimpleChatMessage
					{
						baseToken = "PORTAL_SHOP_WILL_OPEN"
					});
				}
			}
		}
	}

	public bool shouldAttemptToSpawnGoldshoresPortal
	{
		get
		{
			return _shouldAttemptToSpawnGoldshoresPortal;
		}
		set
		{
			if (_shouldAttemptToSpawnGoldshoresPortal != value)
			{
				Network_shouldAttemptToSpawnGoldshoresPortal = value;
				if (_shouldAttemptToSpawnGoldshoresPortal && NetworkServer.active)
				{
					Chat.SendBroadcastChat(new Chat.SimpleChatMessage
					{
						baseToken = "PORTAL_GOLDSHORES_WILL_OPEN"
					});
				}
			}
		}
	}

	public bool shouldAttemptToSpawnMSPortal
	{
		get
		{
			return _shouldAttemptToSpawnMSPortal;
		}
		set
		{
			if (_shouldAttemptToSpawnMSPortal != value)
			{
				Network_shouldAttemptToSpawnMSPortal = value;
				if (_shouldAttemptToSpawnMSPortal && NetworkServer.active)
				{
					Chat.SendBroadcastChat(new Chat.SimpleChatMessage
					{
						baseToken = "PORTAL_MS_WILL_OPEN"
					});
				}
			}
		}
	}

	public bool Network_locked
	{
		get
		{
			return _locked;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _locked, 1u);
		}
	}

	public bool NetworkisDiscovered
	{
		get
		{
			return isDiscovered;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncTeleporterDiscovered(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref isDiscovered, 2u);
		}
	}

	public bool NetworkisActivated
	{
		get
		{
			return isActivated;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref isActivated, 4u);
		}
	}

	public bool Network_shouldAttemptToSpawnShopPortal
	{
		get
		{
			return _shouldAttemptToSpawnShopPortal;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncShouldAttemptToSpawnShopPortal(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref _shouldAttemptToSpawnShopPortal, 8u);
		}
	}

	public bool Network_shouldAttemptToSpawnGoldshoresPortal
	{
		get
		{
			return _shouldAttemptToSpawnGoldshoresPortal;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncShouldAttemptToSpawnGoldshoresPortal(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref _shouldAttemptToSpawnGoldshoresPortal, 16u);
		}
	}

	public bool Network_shouldAttemptToSpawnMSPortal
	{
		get
		{
			return _shouldAttemptToSpawnMSPortal;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncShouldAttemptToSpawnMSPortal(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref _shouldAttemptToSpawnMSPortal, 32u);
		}
	}

	public bool NetworkshowExtraBossesIndicator
	{
		get
		{
			return showExtraBossesIndicator;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref showExtraBossesIndicator, 64u);
		}
	}

	public static event Action<TeleporterInteraction> onTeleporterBeginChargingGlobal;

	public static event Action<TeleporterInteraction> onTeleporterChargedGlobal;

	public static event Action<TeleporterInteraction> onTeleporterFinishGlobal;

	private void OnSyncShouldAttemptToSpawnShopPortal(bool newValue)
	{
		Network_shouldAttemptToSpawnShopPortal = newValue;
		if ((bool)modelChildLocator)
		{
			modelChildLocator.FindChild("ShopPortalIndicator").gameObject.SetActive(newValue);
		}
	}

	private void OnSyncShouldAttemptToSpawnGoldshoresPortal(bool newValue)
	{
		Network_shouldAttemptToSpawnGoldshoresPortal = newValue;
		if ((bool)modelChildLocator)
		{
			modelChildLocator.FindChild("GoldshoresPortalIndicator").gameObject.SetActive(newValue);
		}
	}

	private void OnSyncShouldAttemptToSpawnMSPortal(bool newValue)
	{
		Network_shouldAttemptToSpawnMSPortal = newValue;
		if ((bool)modelChildLocator)
		{
			modelChildLocator.FindChild("MSPortalIndicator").gameObject.SetActive(newValue);
		}
	}

	private void OnSyncTeleporterDiscovered(bool newValue)
	{
		NetworkisDiscovered = newValue;
		ChargeIndicatorController component = teleporterPositionIndicator.GetComponent<ChargeIndicatorController>();
		if ((object)component != null)
		{
			component.isDiscovered = newValue;
			if (newValue)
			{
				component.TriggerPingAnimation();
			}
		}
	}

	private void Awake()
	{
		modelChildLocator = GetComponent<ModelLocator>().modelTransform.GetComponent<ChildLocator>();
		holdoutZoneController = GetComponent<HoldoutZoneController>();
		sceneExitController = GetComponent<SceneExitController>();
		bossShrineIndicator = modelChildLocator.FindChild("BossShrineSymbol").gameObject;
		bossGroup = GetComponent<BossGroup>();
		if (NetworkServer.active && (bool)bossDirector)
		{
			bossDirector.onSpawnedServer.AddListener(OnBossDirectorSpawnedMonsterServer);
		}
		teleporterPositionIndicator = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/PositionIndicators/TeleporterChargingPositionIndicator"), base.transform.position, Quaternion.identity).GetComponent<PositionIndicator>();
		teleporterPositionIndicator.targetTransform = base.transform;
		teleporterChargeIndicatorController = teleporterPositionIndicator.GetComponent<ChargeIndicatorController>();
		teleporterChargeIndicatorController.holdoutZoneController = holdoutZoneController;
		teleporterPositionIndicator.gameObject.SetActive(value: false);
		Particle_SetMinSize component = modelChildLocator.FindChild("PassiveParticleSphere").GetComponent<Particle_SetMinSize>();
		if ((object)component != null)
		{
			cachedParticleMinSizeScript = component;
		}
		else
		{
			Debug.LogWarning("TeleporterInteraction could not get PassiveParticleSphere's Particle_SetMinSize. Make sure it's set in the Child Locator");
		}
		cachedLocalUser = ((MPEventSystem)EventSystem.current)?.localUser;
	}

	private void OnDestroy()
	{
		if ((bool)teleporterPositionIndicator)
		{
			UnityEngine.Object.Destroy(teleporterPositionIndicator.gameObject);
			teleporterPositionIndicator = null;
			teleporterChargeIndicatorController = null;
		}
	}

	private void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
		InstanceTracker.Add(this);
		sceneStartAmbientLevel = Run.instance.ambientLevel;
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
		instance = SingletonHelper.Unassign(instance, this);
	}

	private void Start()
	{
		if (NetworkServer.active)
		{
			rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUlong);
			Run.instance.PickNextStageSceneFromCurrentSceneDestinations();
			float nextNormalizedFloat = rng.nextNormalizedFloat;
			float num = baseShopSpawnChance / (float)(Run.instance.shopPortalCount + 1);
			shouldAttemptToSpawnShopPortal = nextNormalizedFloat < num;
			int stageClearCount = Run.instance.stageClearCount;
			if ((stageClearCount + 1) % Run.stagesPerLoop == 3 && stageClearCount > Run.stagesPerLoop && !Run.instance.GetEventFlag("NoMysterySpace"))
			{
				shouldAttemptToSpawnMSPortal = true;
			}
		}
		if (base.isClient)
		{
			OnSyncTeleporterDiscovered(isDiscovered);
			teleporterPositionIndicator.GetComponent<ChargeIndicatorController>().isActivated = isActivated;
			teleporterPositionIndicator.GetComponent<ChargeIndicatorController>().isCharged = isCharged;
		}
	}

	public void FixedUpdate()
	{
		bossShrineIndicator.SetActive(showExtraBossesIndicator && !isCharged);
		teleporterPositionIndicator.gameObject.SetActive(!isIdle || (isDiscovered && cachedLocalUser.userProfile.useTeleporterDiscoveryIndicator) || currentPings.Count > 0);
		if (base.hasAuthority && !isDiscovered && HoldoutZoneController.CountPlayersInRadius(holdoutZoneController, base.transform.position, discoveryRadius * discoveryRadius, TeamIndex.Player) > 0)
		{
			NetworkisDiscovered = true;
			teleporterPositionIndicator.GetComponent<ChargeIndicatorController>().isDiscovered = true;
			teleporterPositionIndicator.GetComponent<ChargeIndicatorController>().TriggerPingAnimation();
		}
		if (cachedLocalUser != null && cachedLocalUser.userProfile.useTeleporterParticleScaling)
		{
			shouldScaleParticles = true;
			particleScaleUpdateTimer -= Time.fixedDeltaTime;
			if (particleScaleUpdateTimer <= 0f)
			{
				particleScaleUpdateTimer = 0.5f;
				cachedParticleMinSizeScript.SetEnabled(isEnabled: true);
			}
		}
		else if (shouldScaleParticles)
		{
			shouldScaleParticles = false;
			cachedParticleMinSizeScript.SetEnabled(isEnabled: false);
		}
	}

	public string GetContextString(Interactor activator)
	{
		return currentState?.GetContextString(activator);
	}

	public Interactability GetInteractability(Interactor activator)
	{
		if (locked)
		{
			return Interactability.Disabled;
		}
		return currentState?.GetInteractability(activator) ?? Interactability.Disabled;
	}

	public void OnInteractionBegin(Interactor activator)
	{
		CallRpcClientOnActivated(activator.gameObject);
		currentState?.OnInteractionBegin(activator);
	}

	public bool ShouldShowOnScanner()
	{
		return true;
	}

	public bool ShouldProximityHighlight()
	{
		return true;
	}

	public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
	{
		return false;
	}

	[ConCommand(commandName = "activate_teleporter", flags = ConVarFlags.None, helpText = "Activates teleporter")]
	private static void ActivateTeleporter(ConCommandArgs args)
	{
		Interactor component = TeamComponent.GetTeamMembers(TeamIndex.Player)[0].GetComponent<Interactor>();
		if ((bool)component)
		{
			instance.CallRpcClientOnActivated(component.gameObject);
			instance.currentState?.OnInteractionBegin(component);
			CharacterBody component2 = component.GetComponent<CharacterBody>();
			if ((bool)component2)
			{
				TeleportHelper.TeleportBody(component2, instance.transform.position);
			}
		}
	}

	[ClientRpc]
	private void RpcClientOnActivated(GameObject activatorObject)
	{
		Util.PlaySound("Play_env_teleporter_active_button", base.gameObject);
	}

	private void UpdateMonstersClear()
	{
		monstersCleared = !bossGroup.enabled;
	}

	[Server]
	public void AddShrineStack()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.TeleporterInteraction::AddShrineStack()' called on client");
		}
		else if (activationState <= ActivationState.IdleToCharging)
		{
			BossGroup obj = bossGroup;
			int bonusRewardCount = obj.bonusRewardCount + 1;
			obj.bonusRewardCount = bonusRewardCount;
			shrineBonusStacks++;
			NetworkshowExtraBossesIndicator = true;
		}
	}

	[Obsolete]
	public bool IsInChargingRange(CharacterBody body)
	{
		return holdoutZoneController.IsBodyInChargingRadius(body);
	}

	private void OnBossDirectorSpawnedMonsterServer(GameObject masterObject)
	{
		BaseAI ai;
		if ((bool)chargeActivatorServer)
		{
			ai = masterObject.GetComponent<BaseAI>();
			if ((bool)ai)
			{
				ai.onBodyDiscovered += AiOnBodyDiscovered;
			}
		}
		void AiOnBodyDiscovered(CharacterBody newBody)
		{
			ai.currentEnemy.gameObject = chargeActivatorServer;
			ai.onBodyDiscovered -= AiOnBodyDiscovered;
		}
	}

	[Server]
	private void AttemptToSpawnAllEligiblePortals()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.TeleporterInteraction::AttemptToSpawnAllEligiblePortals()' called on client");
			return;
		}
		if (shouldAttemptToSpawnShopPortal)
		{
			AttemptToSpawnShopPortal();
		}
		if (shouldAttemptToSpawnGoldshoresPortal)
		{
			AttemptToSpawnGoldshoresPortal();
		}
		if (shouldAttemptToSpawnMSPortal)
		{
			AttemptToSpawnMSPortal();
		}
		PortalSpawner[] array = portalSpawners;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].AttemptSpawnPortalServer();
		}
	}

	private bool AttemptSpawnPortal(SpawnCard portalSpawnCard, float minDistance, float maxDistance, string successChatToken)
	{
		GameObject obj = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(portalSpawnCard, new DirectorPlacementRule
		{
			minDistance = minDistance,
			maxDistance = maxDistance,
			placementMode = DirectorPlacementRule.PlacementMode.Approximate,
			position = base.transform.position,
			spawnOnTarget = base.transform
		}, rng));
		if ((bool)obj)
		{
			Chat.SendBroadcastChat(new Chat.SimpleChatMessage
			{
				baseToken = successChatToken
			});
		}
		return obj;
	}

	[Server]
	private void AttemptToSpawnShopPortal()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.TeleporterInteraction::AttemptToSpawnShopPortal()' called on client");
		}
		else if (AttemptSpawnPortal(shopPortalSpawnCard, 0f, 30f, "PORTAL_SHOP_OPEN"))
		{
			Run.instance.shopPortalCount++;
		}
	}

	[Server]
	private void AttemptToSpawnGoldshoresPortal()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.TeleporterInteraction::AttemptToSpawnGoldshoresPortal()' called on client");
		}
		else
		{
			AttemptSpawnPortal(goldshoresPortalSpawnCard, 10f, 40f, "PORTAL_GOLDSHORES_OPEN");
		}
	}

	[Server]
	private void AttemptToSpawnMSPortal()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.TeleporterInteraction::AttemptToSpawnMSPortal()' called on client");
		}
		else
		{
			AttemptSpawnPortal(msPortalSpawnCard, 10f, 40f, "PORTAL_MS_OPEN");
		}
	}

	public void PingTeleporter(string pingOwnerName, PingIndicator pingObject)
	{
		if ((bool)pingObject)
		{
			if (base.hasAuthority)
			{
				NetworkisDiscovered = true;
			}
			currentPings.Add(pingObject);
			teleporterPositionIndicator.GetComponent<ChargeIndicatorController>()?.TriggerPing(pingOwnerName);
		}
	}

	public void CancelTeleporterPing(PingIndicator cancelledPingObject)
	{
		if ((bool)cancelledPingObject && currentPings.Contains(cancelledPingObject))
		{
			currentPings.Remove(cancelledPingObject);
		}
		ChargeIndicatorController component = teleporterPositionIndicator.GetComponent<ChargeIndicatorController>();
		if (!component)
		{
			return;
		}
		int num = currentPings.Count - 1;
		while (num >= 0)
		{
			if (!currentPings[num])
			{
				currentPings.RemoveAt(num);
				num--;
				continue;
			}
			component.TriggerPing(currentPings[num].GetOwnerName(), triggerAnimation: false);
			return;
		}
		component.isPinged = false;
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcClientOnActivated(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcClientOnActivated called on server.");
		}
		else
		{
			((TeleporterInteraction)obj).RpcClientOnActivated(reader.ReadGameObject());
		}
	}

	public void CallRpcClientOnActivated(GameObject activatorObject)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcClientOnActivated called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcClientOnActivated);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(activatorObject);
		SendRPCInternal(networkWriter, 0, "RpcClientOnActivated");
	}

	static TeleporterInteraction()
	{
		kRpcRpcClientOnActivated = 1157394167;
		NetworkBehaviour.RegisterRpcDelegate(typeof(TeleporterInteraction), kRpcRpcClientOnActivated, InvokeRpcRpcClientOnActivated);
		NetworkCRC.RegisterBehaviour("TeleporterInteraction", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(_locked);
			writer.Write(isDiscovered);
			writer.Write(isActivated);
			writer.Write(_shouldAttemptToSpawnShopPortal);
			writer.Write(_shouldAttemptToSpawnGoldshoresPortal);
			writer.Write(_shouldAttemptToSpawnMSPortal);
			writer.Write(showExtraBossesIndicator);
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
			writer.Write(_locked);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(isDiscovered);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(isActivated);
		}
		if ((base.syncVarDirtyBits & 8u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_shouldAttemptToSpawnShopPortal);
		}
		if ((base.syncVarDirtyBits & 0x10u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_shouldAttemptToSpawnGoldshoresPortal);
		}
		if ((base.syncVarDirtyBits & 0x20u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_shouldAttemptToSpawnMSPortal);
		}
		if ((base.syncVarDirtyBits & 0x40u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(showExtraBossesIndicator);
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
			_locked = reader.ReadBoolean();
			isDiscovered = reader.ReadBoolean();
			isActivated = reader.ReadBoolean();
			_shouldAttemptToSpawnShopPortal = reader.ReadBoolean();
			_shouldAttemptToSpawnGoldshoresPortal = reader.ReadBoolean();
			_shouldAttemptToSpawnMSPortal = reader.ReadBoolean();
			showExtraBossesIndicator = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			_locked = reader.ReadBoolean();
		}
		if (((uint)num & 2u) != 0)
		{
			OnSyncTeleporterDiscovered(reader.ReadBoolean());
		}
		if (((uint)num & 4u) != 0)
		{
			isActivated = reader.ReadBoolean();
		}
		if (((uint)num & 8u) != 0)
		{
			OnSyncShouldAttemptToSpawnShopPortal(reader.ReadBoolean());
		}
		if (((uint)num & 0x10u) != 0)
		{
			OnSyncShouldAttemptToSpawnGoldshoresPortal(reader.ReadBoolean());
		}
		if (((uint)num & 0x20u) != 0)
		{
			OnSyncShouldAttemptToSpawnMSPortal(reader.ReadBoolean());
		}
		if (((uint)num & 0x40u) != 0)
		{
			showExtraBossesIndicator = reader.ReadBoolean();
		}
	}

	public override void PreStartClient()
	{
	}
}

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using RoR2.ConVar;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(NetworkRuleBook))]
[RequireComponent(typeof(NetworkRuleChoiceMask))]
public class PreGameController : NetworkBehaviour
{
	private enum PregameState
	{
		Idle,
		Launching,
		Launched
	}

	public class GameModeConVar : BaseConVar
	{
		public static readonly GameModeConVar instance;

		public Run runPrefabComponent;

		public GameModeConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		static GameModeConVar()
		{
			instance = new GameModeConVar("gamemode", ConVarFlags.None, "ClassicRun", "Sets the specified game mode as the one to use in the next run.");
			GameModeCatalog.availability.CallWhenAvailable(delegate
			{
				instance.runPrefabComponent = GameModeCatalog.FindGameModePrefabComponent(instance.GetString());
			});
		}

		public override void SetString(string newValue)
		{
			GameModeCatalog.availability.CallWhenAvailable(delegate
			{
				Run run = GameModeCatalog.FindGameModePrefabComponent(newValue);
				if (!run)
				{
					Debug.LogFormat("GameMode \"{0}\" does not exist.", newValue);
				}
				else
				{
					runPrefabComponent = run;
				}
			});
		}

		public override string GetString()
		{
			if (!runPrefabComponent)
			{
				return "ClassicRun";
			}
			return runPrefabComponent.gameObject.name;
		}
	}

	private NetworkRuleChoiceMask networkRuleChoiceMaskComponent;

	private NetworkRuleBook networkRuleBookComponent;

	private RuleChoiceMask serverAvailableChoiceMask;

	public ulong runSeed;

	public static readonly IntConVar overrideRunSeed;

	[SyncVar(hook = "UpdateGameModeIndex")]
	private int _gameModeIndex;

	private GameObject lobbyBackground;

	private int currentLobbyBackgroundGameModeIndex;

	private float lobbyBackgroundTimeToRefresh;

	private const float lobbyBackgroundTimeToRefreshInterval = 4f;

	private RuleBook ruleBookBuffer;

	[SyncVar]
	private int pregameStateInternal;

	private const float launchTransitionDuration = 0f;

	private GameObject gameModePrefab;

	[SyncVar]
	private float launchStartTime = float.PositiveInfinity;

	private RuleChoiceMask unlockedChoiceMask;

	private RuleChoiceMask dependencyChoiceMask;

	private RuleChoiceMask entitlementChoiceMask;

	private RuleChoiceMask requiredExpansionEnabledChoiceMask;

	private RuleChoiceMask choiceMaskBuffer;

	public static readonly BoolConVar cvSvAllowRuleVoting;

	public static readonly BoolConVar cvSvAllowMultiplayerPause;

	private static int kRpcRpcUpdateGameModeIndex;

	private static int kRpcRpcUpdateMultiplayerPause;

	public static PreGameController instance { get; private set; }

	public RuleChoiceMask resolvedRuleChoiceMask => networkRuleChoiceMaskComponent.ruleChoiceMask;

	public RuleBook readOnlyRuleBook => networkRuleBookComponent.ruleBook;

	public GameModeIndex gameModeIndex
	{
		get
		{
			return (GameModeIndex)_gameModeIndex;
		}
		set
		{
			Network_gameModeIndex = (int)value;
		}
	}

	private PregameState pregameState
	{
		get
		{
			return (PregameState)pregameStateInternal;
		}
		set
		{
			NetworkpregameStateInternal = (int)value;
		}
	}

	public int Network_gameModeIndex
	{
		get
		{
			return _gameModeIndex;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				UpdateGameModeIndex(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref _gameModeIndex, 1u);
		}
	}

	public int NetworkpregameStateInternal
	{
		get
		{
			return pregameStateInternal;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref pregameStateInternal, 2u);
		}
	}

	public float NetworklaunchStartTime
	{
		get
		{
			return launchStartTime;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref launchStartTime, 4u);
		}
	}

	public static event Action<PreGameController, RuleBook> onPreGameControllerSetRuleBookServerGlobal;

	public static event Action<PreGameController, RuleBook> onPreGameControllerSetRuleBookGlobal;

	public static event Action<PreGameController> onServerRecalculatedModifierAvailability;

	private void Awake()
	{
		lobbyBackgroundTimeToRefresh = 0f;
		networkRuleChoiceMaskComponent = GetComponent<NetworkRuleChoiceMask>();
		networkRuleBookComponent = GetComponent<NetworkRuleBook>();
		networkRuleBookComponent.onRuleBookUpdated += OnRuleBookUpdated;
		ruleBookBuffer = new RuleBook();
		serverAvailableChoiceMask = new RuleChoiceMask();
		unlockedChoiceMask = new RuleChoiceMask();
		dependencyChoiceMask = new RuleChoiceMask();
		entitlementChoiceMask = new RuleChoiceMask();
		choiceMaskBuffer = new RuleChoiceMask();
		requiredExpansionEnabledChoiceMask = new RuleChoiceMask();
		if (NetworkServer.active)
		{
			gameModeIndex = GameModeCatalog.FindGameModeIndex(GameModeConVar.instance.GetString());
			runSeed = GameModeCatalog.GetGameModePrefabComponent(gameModeIndex).GenerateSeedForNewRun();
		}
		bool isInSinglePlayer = RoR2Application.isInSinglePlayer;
		for (int i = 0; i < serverAvailableChoiceMask.length; i++)
		{
			RuleChoiceDef choiceDef = RuleCatalog.GetChoiceDef(i);
			serverAvailableChoiceMask[i] = (isInSinglePlayer ? choiceDef.availableInSinglePlayer : choiceDef.availableInMultiPlayer);
		}
		NetworkUser.OnPostNetworkUserStart += GenerateRuleVoteController;
		RefreshLobbyBackground();
	}

	private void OnDestroy()
	{
		NetworkUser.OnPostNetworkUserStart -= GenerateRuleVoteController;
	}

	private void GenerateRuleVoteController(NetworkUser networkUser)
	{
		if (NetworkServer.active && !PreGameRuleVoteController.FindForUser(networkUser) && (networkUser.isLocalPlayer || cvSvAllowRuleVoting.value))
		{
			PreGameRuleVoteController.CreateForNetworkUserServer(networkUser);
		}
	}

	private void Start()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		ResolveChoiceMask();
		Console.instance.SubmitCmd(null, "exec server_pregame");
		foreach (NetworkUser readOnlyInstances in NetworkUser.readOnlyInstancesList)
		{
			Debug.LogFormat("Attempting to generate PreGameVoteController for {0}", readOnlyInstances.userName);
			GenerateRuleVoteController(readOnlyInstances);
			readOnlyInstances.BumpDLCChecker();
		}
	}

	[Server]
	public bool ApplyChoice(int ruleChoiceIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.PreGameController::ApplyChoice(System.Int32)' called on client");
			return false;
		}
		if (!resolvedRuleChoiceMask[ruleChoiceIndex])
		{
			return false;
		}
		RuleChoiceDef choiceDef = RuleCatalog.GetChoiceDef(ruleChoiceIndex);
		if (readOnlyRuleBook.GetRuleChoice(choiceDef.ruleDef.globalIndex) == choiceDef)
		{
			return false;
		}
		ruleBookBuffer.Copy(readOnlyRuleBook);
		ruleBookBuffer.ApplyChoice(choiceDef);
		SetRuleBook(ruleBookBuffer);
		return true;
	}

	private void SetRuleBook(RuleBook newRuleBook)
	{
		networkRuleBookComponent.SetRuleBook(newRuleBook);
		PreGameController.onPreGameControllerSetRuleBookServerGlobal?.Invoke(this, readOnlyRuleBook);
	}

	private void OnRuleBookUpdated(NetworkRuleBook networkRuleBookComponent)
	{
		PreGameController.onPreGameControllerSetRuleBookGlobal?.Invoke(this, networkRuleBookComponent.ruleBook);
	}

	[Server]
	public void EnforceValidRuleChoices()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PreGameController::EnforceValidRuleChoices()' called on client");
			return;
		}
		ruleBookBuffer.Copy(readOnlyRuleBook);
		for (int i = 0; i < RuleCatalog.ruleCount; i++)
		{
			RuleChoiceDef ruleChoice = ruleBookBuffer.GetRuleChoice(i);
			if (resolvedRuleChoiceMask[ruleChoice])
			{
				continue;
			}
			RuleDef ruleDef = RuleCatalog.GetRuleDef(i);
			RuleChoiceDef choiceDef = ruleDef.choices[ruleDef.defaultChoiceIndex];
			int num = 0;
			int j = 0;
			for (int count = ruleDef.choices.Count; j < count; j++)
			{
				if (resolvedRuleChoiceMask[ruleDef.choices[j]])
				{
					num++;
				}
			}
			if (resolvedRuleChoiceMask[choiceDef] || num == 0)
			{
				ruleBookBuffer.ApplyChoice(choiceDef);
				continue;
			}
			int k = 0;
			for (int count2 = ruleDef.choices.Count; k < count2; k++)
			{
				if (resolvedRuleChoiceMask[ruleDef.choices[k]])
				{
					ruleBookBuffer.ApplyChoice(ruleDef.choices[k]);
					break;
				}
			}
		}
		SetRuleBook(ruleBookBuffer);
	}

	private void TestRuleValues()
	{
		RuleBook ruleBook = new RuleBook();
		ruleBook.Copy(networkRuleBookComponent.ruleBook);
		RuleDef ruleDef = RuleCatalog.GetRuleDef(UnityEngine.Random.Range(0, RuleCatalog.ruleCount));
		RuleChoiceDef choiceDef = ruleDef.choices[UnityEngine.Random.Range(0, ruleDef.choices.Count)];
		ruleBook.ApplyChoice(choiceDef);
		SetRuleBook(ruleBook);
		Invoke("TestRuleValues", 0.5f);
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		PauseManager.isPaused = false;
		PauseStopController.instance?.ForceClientUnpause();
	}

	private void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
		if (NetworkServer.active)
		{
			RecalculateModifierAvailability();
		}
		NetworkUser.OnNetworkUserUnlockablesUpdated += OnNetworkUserUnlockablesUpdatedCallback;
		NetworkUser.OnPostNetworkUserStart += OnPostNetworkUserStartCallback;
		NetworkUser.onNetworkUserBodyPreferenceChanged += OnNetworkUserBodyPreferenceChanged;
		NetworkUser.onNetworkUserLost += OnNetworkUserLost;
		if (NetworkClient.active)
		{
			foreach (NetworkUser readOnlyLocalPlayers in NetworkUser.readOnlyLocalPlayersList)
			{
				readOnlyLocalPlayers.SendServerUnlockables();
			}
		}
		if (!NetworkServer.active)
		{
			return;
		}
		foreach (NetworkUser readOnlyInstances in NetworkUser.readOnlyInstancesList)
		{
			readOnlyInstances.ServerRequestUnlockables();
		}
	}

	private void OnDisable()
	{
		instance = SingletonHelper.Unassign(instance, this);
		NetworkUser.OnPostNetworkUserStart -= OnPostNetworkUserStartCallback;
		NetworkUser.OnNetworkUserUnlockablesUpdated -= OnNetworkUserUnlockablesUpdatedCallback;
		NetworkUser.onNetworkUserBodyPreferenceChanged -= OnNetworkUserBodyPreferenceChanged;
		NetworkUser.onNetworkUserLost -= OnNetworkUserLost;
	}

	public bool IsCharacterSwitchingCurrentlyAllowed()
	{
		return pregameState == PregameState.Idle;
	}

	private void Update()
	{
		if (pregameState == PregameState.Launching)
		{
			if (PlatformSystems.networkManager.unpredictedServerFixedTime - launchStartTime >= 0.5f && NetworkServer.active)
			{
				StartRun();
			}
		}
		else
		{
			_ = pregameState;
			_ = 2;
		}
		if (NetworkServer.active)
		{
			lobbyBackgroundTimeToRefresh -= Time.deltaTime;
			if (lobbyBackgroundTimeToRefresh <= 0f)
			{
				lobbyBackgroundTimeToRefresh = 4f;
				CallRpcUpdateGameModeIndex(_gameModeIndex);
				CallRpcUpdateMultiplayerPause(cvSvAllowMultiplayerPause.value);
			}
		}
	}

	[Server]
	public void StartLaunch()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PreGameController::StartLaunch()' called on client");
		}
		else if (pregameState == PregameState.Idle)
		{
			pregameState = PregameState.Launching;
			NetworklaunchStartTime = PlatformSystems.networkManager.unpredictedServerFixedTime;
		}
	}

	[Server]
	private void StartRun()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PreGameController::StartRun()' called on client");
			return;
		}
		ExpansionRequirementComponent component = GameModeConVar.instance.runPrefabComponent.GetComponent<ExpansionRequirementComponent>();
		if (!component || !component.requiredExpansion || EntitlementManager.networkUserEntitlementTracker.AnyUserHasEntitlement(component.requiredExpansion.requiredEntitlement))
		{
			pregameState = PregameState.Launched;
			NetworkSession.instance.BeginRun(GameModeConVar.instance.runPrefabComponent, readOnlyRuleBook, runSeed);
		}
	}

	[ConCommand(commandName = "pregame_start_run", flags = ConVarFlags.SenderMustBeServer, helpText = "Begins a run out of pregame.")]
	private static void CCPregameStartRun(ConCommandArgs args)
	{
		if ((bool)instance)
		{
			instance.StartRun();
		}
	}

	private static bool AnyUserHasUnlockable([NotNull] UnlockableDef unlockableDef)
	{
		ReadOnlyCollection<NetworkUser> readOnlyInstancesList = NetworkUser.readOnlyInstancesList;
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			if (readOnlyInstancesList[i].unlockables.Contains(unlockableDef))
			{
				return true;
			}
		}
		return false;
	}

	[ContextMenu("RecalculateModifierAvailability")]
	[Server]
	public void RecalculateModifierAvailability()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PreGameController::RecalculateModifierAvailability()' called on client");
			return;
		}
		for (int i = 0; i < RuleCatalog.choiceCount; i++)
		{
			RuleChoiceDef choiceDef = RuleCatalog.GetChoiceDef(i);
			unlockedChoiceMask[i] = !choiceDef.requiredUnlockable || AnyUserHasUnlockable(choiceDef.requiredUnlockable);
			dependencyChoiceMask[i] = choiceDef.requiredChoiceDef == null || readOnlyRuleBook.IsChoiceActive(choiceDef.requiredChoiceDef);
			entitlementChoiceMask[i] = !choiceDef.requiredEntitlementDef || EntitlementManager.networkUserEntitlementTracker.AnyUserHasEntitlement(choiceDef.requiredEntitlementDef) || EntitlementManager.localUserEntitlementTracker.AnyUserHasEntitlement(choiceDef.requiredEntitlementDef);
			requiredExpansionEnabledChoiceMask[i] = !choiceDef.requiredExpansionDef || readOnlyRuleBook.IsChoiceActive(choiceDef.requiredExpansionDef.enabledChoice);
		}
		ResolveChoiceMask();
		PreGameController.onServerRecalculatedModifierAvailability?.Invoke(this);
	}

	[Server]
	private void ResolveChoiceMask()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PreGameController::ResolveChoiceMask()' called on client");
			return;
		}
		RuleChoiceMask ruleChoiceMask = new RuleChoiceMask();
		RuleChoiceMask ruleChoiceMask2 = new RuleChoiceMask();
		Run gameModePrefabComponent = GameModeCatalog.GetGameModePrefabComponent(gameModeIndex);
		if ((bool)gameModePrefabComponent)
		{
			if (overrideRunSeed.value != -1)
			{
				runSeed = (ulong)overrideRunSeed.value;
			}
			gameModePrefabComponent.OverrideRuleChoices(ruleChoiceMask, ruleChoiceMask2, runSeed);
		}
		for (int i = 0; i < choiceMaskBuffer.length; i++)
		{
			RuleChoiceDef choiceDef = RuleCatalog.GetChoiceDef(i);
			bool flag = ruleChoiceMask[i];
			bool flag2 = ruleChoiceMask2[i];
			bool flag3 = serverAvailableChoiceMask[i];
			bool flag4 = unlockedChoiceMask[i];
			bool flag5 = dependencyChoiceMask[i];
			bool flag6 = entitlementChoiceMask[i];
			bool flag7 = requiredExpansionEnabledChoiceMask[i];
			choiceMaskBuffer[i] = flag || (!flag2 && flag3 && flag4 && flag5 && flag6 && flag7 && !choiceDef.excludeByDefault);
		}
		networkRuleChoiceMaskComponent.SetRuleChoiceMask(choiceMaskBuffer);
		EnforceValidRuleChoices();
	}

	private void OnNetworkUserUnlockablesUpdatedCallback(NetworkUser networkUser)
	{
		if (NetworkServer.active)
		{
			RecalculateModifierAvailability();
		}
	}

	private void OnPostNetworkUserStartCallback(NetworkUser networkUser)
	{
		if (NetworkServer.active)
		{
			networkUser.ServerRequestUnlockables();
		}
	}

	private void OnNetworkUserBodyPreferenceChanged(NetworkUser networkUser)
	{
		if (NetworkServer.active)
		{
			RecalculateModifierAvailability();
		}
	}

	private void OnNetworkUserLost(NetworkUser networkUser)
	{
		if (NetworkServer.active)
		{
			RecalculateModifierAvailability();
		}
	}

	private void UpdateGameModeIndex(int newIndex)
	{
		if (newIndex != currentLobbyBackgroundGameModeIndex)
		{
			Network_gameModeIndex = newIndex;
			RefreshLobbyBackground();
		}
	}

	private void UpdateMultiplayerPause(bool newBool)
	{
		if ((bool)PauseStopController.instance)
		{
			PauseStopController.instance.allowMultiplayerPause = newBool;
		}
	}

	[ClientRpc]
	private void RpcUpdateGameModeIndex(int newIndex)
	{
		UpdateGameModeIndex(newIndex);
	}

	[ClientRpc]
	private void RpcUpdateMultiplayerPause(bool newBool)
	{
		UpdateMultiplayerPause(newBool);
	}

	private void RefreshLobbyBackground()
	{
		if ((bool)lobbyBackground)
		{
			UnityEngine.Object.Destroy(lobbyBackground);
		}
		currentLobbyBackgroundGameModeIndex = _gameModeIndex;
		lobbyBackground = UnityEngine.Object.Instantiate(GameModeCatalog.GetGameModePrefabComponent(gameModeIndex).lobbyBackgroundPrefab);
	}

	[ConCommand(commandName = "pregame_set_rule_choice", flags = ConVarFlags.SenderMustBeServer, helpText = "Sets the specified choice during pregame. See the command \"rule_list_choices\" for possible options.")]
	public static void CCPregameSetRuleChoice(ConCommandArgs args)
	{
		string argString = args.GetArgString(0);
		RuleChoiceDef ruleChoiceDef = RuleCatalog.FindChoiceDef(argString);
		if (ruleChoiceDef == null)
		{
			throw new ConCommandException($"'{argString}' is not a recognized rule choice name.");
		}
		if (!instance)
		{
			throw new ConCommandException("This command cannot be issued outside the character select screen.");
		}
		if (instance.ApplyChoice(ruleChoiceDef.globalIndex))
		{
			instance.RecalculateModifierAvailability();
		}
	}

	static PreGameController()
	{
		overrideRunSeed = new IntConVar("override_run_seed", ConVarFlags.None, "-1", "Forces a default seed for benchmarking.");
		cvSvAllowRuleVoting = new BoolConVar("sv_allow_rule_voting", ConVarFlags.None, "1", "Whether or not players are allowed to vote on rules.");
		cvSvAllowMultiplayerPause = new BoolConVar("sv_allow_multiplayer_pause", ConVarFlags.None, "0", "Whether or not players are allowed to pause the multiplayer game.");
		kRpcRpcUpdateGameModeIndex = -600953683;
		NetworkBehaviour.RegisterRpcDelegate(typeof(PreGameController), kRpcRpcUpdateGameModeIndex, InvokeRpcRpcUpdateGameModeIndex);
		kRpcRpcUpdateMultiplayerPause = 2048931628;
		NetworkBehaviour.RegisterRpcDelegate(typeof(PreGameController), kRpcRpcUpdateMultiplayerPause, InvokeRpcRpcUpdateMultiplayerPause);
		NetworkCRC.RegisterBehaviour("PreGameController", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcUpdateGameModeIndex(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUpdateGameModeIndex called on server.");
		}
		else
		{
			((PreGameController)obj).RpcUpdateGameModeIndex((int)reader.ReadPackedUInt32());
		}
	}

	protected static void InvokeRpcRpcUpdateMultiplayerPause(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUpdateMultiplayerPause called on server.");
		}
		else
		{
			((PreGameController)obj).RpcUpdateMultiplayerPause(reader.ReadBoolean());
		}
	}

	public void CallRpcUpdateGameModeIndex(int newIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcUpdateGameModeIndex called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcUpdateGameModeIndex);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32((uint)newIndex);
		SendRPCInternal(networkWriter, 0, "RpcUpdateGameModeIndex");
	}

	public void CallRpcUpdateMultiplayerPause(bool newBool)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcUpdateMultiplayerPause called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcUpdateMultiplayerPause);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(newBool);
		SendRPCInternal(networkWriter, 0, "RpcUpdateMultiplayerPause");
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.WritePackedUInt32((uint)_gameModeIndex);
			writer.WritePackedUInt32((uint)pregameStateInternal);
			writer.Write(launchStartTime);
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
			writer.WritePackedUInt32((uint)_gameModeIndex);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32((uint)pregameStateInternal);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(launchStartTime);
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
			_gameModeIndex = (int)reader.ReadPackedUInt32();
			pregameStateInternal = (int)reader.ReadPackedUInt32();
			launchStartTime = reader.ReadSingle();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			UpdateGameModeIndex((int)reader.ReadPackedUInt32());
		}
		if (((uint)num & 2u) != 0)
		{
			pregameStateInternal = (int)reader.ReadPackedUInt32();
		}
		if (((uint)num & 4u) != 0)
		{
			launchStartTime = reader.ReadSingle();
		}
	}

	public override void PreStartClient()
	{
	}
}

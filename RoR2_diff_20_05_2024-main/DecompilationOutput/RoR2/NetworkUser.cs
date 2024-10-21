using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using Rewired;
using RoR2.Networking;
using RoR2.Stats;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(NetworkLoadout))]
public class NetworkUser : NetworkBehaviour
{
	public delegate void NetworkUserGenericDelegate(NetworkUser networkUser);

	private static readonly List<NetworkUser> instancesList;

	public static readonly ReadOnlyCollection<NetworkUser> readOnlyInstancesList;

	private static readonly List<NetworkUser> localPlayers;

	public static readonly ReadOnlyCollection<NetworkUser> readOnlyLocalPlayersList;

	[SyncVar(hook = "OnSyncId")]
	private NetworkUserId _id;

	[SyncVar]
	public byte rewiredPlayerId;

	[SyncVar(hook = "OnSyncMasterObjectId")]
	private NetworkInstanceId _masterObjectId;

	[CanBeNull]
	public LocalUser localUser;

	public CameraRigController cameraRigController;

	public string userName = "";

	[SyncVar]
	public Color32 userColor = Color.red;

	[SyncVar]
	private uint netLunarCoins;

	[SyncVar]
	public ItemIndex rebirthItem = ItemIndex.None;

	private MemoizedGetComponent<CharacterMaster> cachedMaster;

	private MemoizedGetComponent<PlayerCharacterMasterController> cachedPlayerCharacterMasterController;

	private MemoizedGetComponent<PlayerStatsComponent> cachedPlayerStatsComponent;

	private GameObject _masterObject;

	[NonSerialized]
	[SyncVar(hook = "SetBodyPreference")]
	public BodyIndex bodyIndexPreference = BodyIndex.None;

	private float secondAccumulator;

	[NonSerialized]
	public List<UnlockableDef> unlockables = new List<UnlockableDef>();

	public List<string> debugUnlockablesList = new List<string>();

	private static NetworkInstanceId serverCurrentStage;

	private NetworkInstanceId _serverLastStageAcknowledgedByClient;

	private static int kRpcRpcDeductLunarCoins;

	private static int kCmdCmdAwardLunarCoins;

	private static int kCmdCmdStoreRebirthItems;

	private static int kRpcRpcStoreRebirthItems;

	private static int kCmdCmdSetRebirthItems;

	private static int kRpcRpcAwardLunarCoins;

	private static int kCmdCmdSetNetLunarCoins;

	private static int kCmdCmdSetBodyPreference;

	private static int kCmdCmdSendConsoleCommand;

	private static int kCmdCmdSendNewUnlockables;

	private static int kRpcRpcRequestUnlockables;

	private static int kCmdCmdReportAchievement;

	private static int kCmdCmdReportUnlock;

	private static int kCmdCmdAcknowledgeStage;

	private static int kCmdCmdSubmitVote;

	public NetworkLoadout networkLoadout { get; private set; }

	public NetworkUserId id
	{
		get
		{
			return _id;
		}
		set
		{
			if (!_id.Equals(value))
			{
				Network_id = value;
				UpdateUserName();
			}
		}
	}

	public bool authed => id.HasValidValue();

	public Player inputPlayer => localUser?.inputPlayer;

	public uint lunarCoins
	{
		get
		{
			if (localUser != null)
			{
				return localUser.userProfile.coins;
			}
			return netLunarCoins;
		}
	}

	public CharacterMaster master => cachedMaster.Get(masterObject);

	public PlayerCharacterMasterController masterController => cachedPlayerCharacterMasterController.Get(masterObject);

	public PlayerStatsComponent masterPlayerStatsComponent => cachedPlayerStatsComponent.Get(masterObject);

	public GameObject masterObject
	{
		get
		{
			if (!_masterObject)
			{
				_masterObject = Util.FindNetworkObject(_masterObjectId);
			}
			return _masterObject;
		}
		set
		{
			if ((bool)value)
			{
				Network_masterObjectId = value.GetComponent<NetworkIdentity>().netId;
				_masterObject = value;
			}
			else
			{
				Network_masterObjectId = NetworkInstanceId.Invalid;
				_masterObject = null;
			}
		}
	}

	public bool isParticipating => masterObject;

	public bool isSplitScreenExtraPlayer => id.subId != 0;

	public bool serverIsClientLoaded { get; private set; }

	private NetworkInstanceId serverLastStageAcknowledgedByClient
	{
		get
		{
			return _serverLastStageAcknowledgedByClient;
		}
		set
		{
			if (!(_serverLastStageAcknowledgedByClient == value))
			{
				_serverLastStageAcknowledgedByClient = value;
				if (serverCurrentStage == _serverLastStageAcknowledgedByClient)
				{
					serverIsClientLoaded = true;
					NetworkUser.onNetworkUserLoadedSceneServer?.Invoke(this);
				}
			}
		}
	}

	public NetworkUserId Network_id
	{
		get
		{
			return _id;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncId(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref _id, 1u);
		}
	}

	public byte NetworkrewiredPlayerId
	{
		get
		{
			return rewiredPlayerId;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref rewiredPlayerId, 2u);
		}
	}

	public NetworkInstanceId Network_masterObjectId
	{
		get
		{
			return _masterObjectId;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncMasterObjectId(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref _masterObjectId, 4u);
		}
	}

	public Color32 NetworkuserColor
	{
		get
		{
			return userColor;
		}
		[param: In]
		set
		{
			SetSyncVar<Color32>(value, ref userColor, 8u);
		}
	}

	public uint NetworknetLunarCoins
	{
		get
		{
			return netLunarCoins;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref netLunarCoins, 16u);
		}
	}

	public ItemIndex NetworkrebirthItem
	{
		get
		{
			return rebirthItem;
		}
		[param: In]
		set
		{
			ulong newValueAsUlong = (ulong)value;
			ulong fieldValueAsUlong = (ulong)rebirthItem;
			SetSyncVarEnum(value, newValueAsUlong, ref rebirthItem, fieldValueAsUlong, 32u);
		}
	}

	public BodyIndex NetworkbodyIndexPreference
	{
		get
		{
			return bodyIndexPreference;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				SetBodyPreference(value);
				base.syncVarHookGuard = false;
			}
			ulong newValueAsUlong = (ulong)value;
			ulong fieldValueAsUlong = (ulong)bodyIndexPreference;
			SetSyncVarEnum(value, newValueAsUlong, ref bodyIndexPreference, fieldValueAsUlong, 64u);
		}
	}

	public static event Action<NetworkUser> onNetworkUserLoadedSceneServer;

	public static event Action<NetworkUser> onLoadoutChangedGlobal;

	[Obsolete("Use onPostNetworkUserStart instead", false)]
	public static event NetworkUserGenericDelegate OnPostNetworkUserStart
	{
		add
		{
			onPostNetworkUserStart += value;
		}
		remove
		{
			onPostNetworkUserStart -= value;
		}
	}

	public static event NetworkUserGenericDelegate onPostNetworkUserStart;

	[Obsolete("Use onNetworkUserUnlockablesUpdated instead", false)]
	public static event NetworkUserGenericDelegate OnNetworkUserUnlockablesUpdated
	{
		add
		{
			onNetworkUserUnlockablesUpdated += value;
		}
		remove
		{
			onNetworkUserUnlockablesUpdated -= value;
		}
	}

	public static event NetworkUserGenericDelegate onNetworkUserUnlockablesUpdated;

	public static event NetworkUserGenericDelegate onNetworkUserDiscovered;

	public static event NetworkUserGenericDelegate onNetworkUserLost;

	public static event NetworkUserGenericDelegate onNetworkUserBodyPreferenceChanged;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Init()
	{
		UserProfile.onUnlockableGranted += delegate(UserProfile userProfile, UnlockableDef unlockableDef)
		{
			if (NetworkClient.active)
			{
				NetworkUser networkUser = FindNetworkUserByUserProfile(userProfile);
				if ((bool)networkUser)
				{
					networkUser.SendServerUnlockables();
				}
			}
		};
		UserProfile.onLoadoutChangedGlobal += delegate(UserProfile userProfile)
		{
			if (NetworkClient.active)
			{
				NetworkUser networkUser2 = FindNetworkUserByUserProfile(userProfile);
				if ((bool)networkUser2)
				{
					networkUser2.PullLoadoutFromUserProfile();
				}
			}
		};
		UserProfile.onSurvivorPreferenceChangedGlobal += delegate(UserProfile userProfile)
		{
			if (NetworkClient.active)
			{
				NetworkUser networkUser3 = FindNetworkUserByUserProfile(userProfile);
				if ((bool)networkUser3)
				{
					networkUser3.SetSurvivorPreferenceClient(userProfile.GetSurvivorPreference());
				}
			}
		};
		Stage.onStageStartGlobal += delegate(Stage stage)
		{
			if (NetworkServer.active)
			{
				serverCurrentStage = stage.netId;
			}
			foreach (NetworkUser localPlayer in localPlayers)
			{
				localPlayer.CallCmdAcknowledgeStage(stage.netId);
			}
		};
	}

	private void OnEnable()
	{
		instancesList.Add(this);
		NetworkUser.onNetworkUserDiscovered?.Invoke(this);
	}

	private void OnDisable()
	{
		NetworkUser.onNetworkUserLost?.Invoke(this);
		instancesList.Remove(this);
	}

	private void Awake()
	{
		UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		networkLoadout = GetComponent<NetworkLoadout>();
		networkLoadout.onLoadoutUpdated += OnLoadoutUpdated;
	}

	private void OnLoadoutUpdated()
	{
		NetworkUser.onLoadoutChangedGlobal?.Invoke(this);
	}

	private void PullLoadoutFromUserProfile()
	{
		UserProfile userProfile = localUser?.userProfile;
		if (userProfile != null)
		{
			Loadout loadout = Loadout.RequestInstance();
			userProfile.CopyLoadout(loadout);
			networkLoadout.SetLoadout(loadout);
			Loadout.ReturnInstance(loadout);
		}
	}

	private void Start()
	{
		if (base.isLocalPlayer)
		{
			LocalUserManager.FindLocalUser(base.playerControllerId)?.LinkNetworkUser(this);
			PullLoadoutFromUserProfile();
			if (!Run.instance || bodyIndexPreference == BodyIndex.None)
			{
				SetSurvivorPreferenceClient(localUser.userProfile.GetSurvivorPreference());
			}
		}
		if ((bool)Run.instance)
		{
			Run.instance.OnUserAdded(this);
		}
		if (NetworkClient.active)
		{
			SyncLunarCoinsToServer();
			SyncRebirthItemsToServer();
			SendServerUnlockables();
		}
		OnLoadoutUpdated();
		NetworkUser.onPostNetworkUserStart?.Invoke(this);
		if (base.isLocalPlayer && (bool)Stage.instance)
		{
			CallCmdAcknowledgeStage(Stage.instance.netId);
		}
	}

	private void OnDestroy()
	{
		localPlayers.Remove(this);
		Run.instance?.OnUserRemoved(this);
		RunCameraManager.instance?.OnUserRemoved(this);
		localUser?.UnlinkNetworkUser();
	}

	public void BumpDLCChecker()
	{
		NetworkUser.onPostNetworkUserStart(this);
	}

	public override void OnStartLocalPlayer()
	{
		base.OnStartLocalPlayer();
		if (!localPlayers.Contains(this))
		{
			localPlayers.Add(this);
		}
		else
		{
			Debug.LogError("OnStartLocalPlayer Already has THIS local player!");
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"OnStartLocalPlayer LOCAL_PLAYERS COUNT {localPlayers.Count}:");
		foreach (NetworkUser localPlayer in localPlayers)
		{
			stringBuilder.AppendLine("\t" + localPlayer.id.strValue);
		}
	}

	public override void OnStartClient()
	{
		UpdateUserName();
	}

	private void OnSyncId(NetworkUserId newId)
	{
		id = newId;
	}

	private void OnSyncMasterObjectId(NetworkInstanceId newValue)
	{
		_masterObject = null;
		Network_masterObjectId = newValue;
	}

	public NetworkPlayerName GetNetworkPlayerName()
	{
		NetworkPlayerName result = default(NetworkPlayerName);
		result.nameOverride = ((id.strValue != null) ? id.strValue : null);
		result.playerId = ((!string.IsNullOrEmpty(id.strValue)) ? default(PlatformID) : new PlatformID(id.value));
		return result;
	}

	[Server]
	public void DeductLunarCoins(uint count)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.NetworkUser::DeductLunarCoins(System.UInt32)' called on client");
			return;
		}
		NetworknetLunarCoins = HGMath.UintSafeSubtract(netLunarCoins, count);
		CallRpcDeductLunarCoins(count);
	}

	[ClientRpc]
	private void RpcDeductLunarCoins(uint count)
	{
		if (localUser != null)
		{
			localUser.userProfile.coins = HGMath.UintSafeSubtract(localUser.userProfile.coins, count);
			SyncLunarCoinsToServer();
		}
	}

	[Server]
	public void AwardLunarCoins(uint count)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.NetworkUser::AwardLunarCoins(System.UInt32)' called on client");
			return;
		}
		NetworknetLunarCoins = HGMath.UintSafeAdd(netLunarCoins, count);
		CallRpcAwardLunarCoins(count);
	}

	[Command]
	public void CmdAwardLunarCoins(uint count)
	{
		AwardLunarCoins(count);
	}

	[Command]
	public void CmdStoreRebirthItems(ItemIndex item)
	{
		StoreRebirthItems(item);
	}

	[Server]
	private void StoreRebirthItems(ItemIndex item)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.NetworkUser::StoreRebirthItems(RoR2.ItemIndex)' called on client");
			return;
		}
		NetworkrebirthItem = item;
		CallRpcStoreRebirthItems(item);
	}

	[ClientRpc]
	private void RpcStoreRebirthItems(ItemIndex item)
	{
		if (localUser != null)
		{
			localUser.userProfile.RebirthItem = PickupCatalog.GetPickupDef(PickupCatalog.FindPickupIndex(item))?.internalName;
			SyncRebirthItemsToServer();
		}
	}

	[Client]
	private void SyncRebirthItemsToServer()
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void RoR2.NetworkUser::SyncRebirthItemsToServer()' called on server");
		}
		else if (localUser != null)
		{
			UserProfile userProfile = localUser.userProfile;
			if (!string.IsNullOrEmpty(userProfile.RebirthItem))
			{
				CallCmdSetRebirthItems((userProfile.RebirthItem != null) ? PickupCatalog.GetPickupDef(PickupCatalog.FindPickupIndex(userProfile.RebirthItem)).itemIndex : ItemIndex.None);
			}
		}
	}

	[Command]
	private void CmdSetRebirthItems(ItemIndex item)
	{
		NetworkrebirthItem = item;
	}

	[ClientRpc]
	private void RpcAwardLunarCoins(uint count)
	{
		if (localUser != null)
		{
			_ = localUser.userProfile.name;
			localUser.userProfile.coins = HGMath.UintSafeAdd(localUser.userProfile.coins, count);
			localUser.userProfile.totalCollectedCoins = HGMath.UintSafeAdd(localUser.userProfile.totalCollectedCoins, count);
			SyncLunarCoinsToServer();
		}
	}

	[Client]
	private void SyncLunarCoinsToServer()
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void RoR2.NetworkUser::SyncLunarCoinsToServer()' called on server");
		}
		else if (localUser != null)
		{
			CallCmdSetNetLunarCoins(localUser.userProfile.coins);
		}
	}

	[Command]
	private void CmdSetNetLunarCoins(uint newNetLunarCoins)
	{
		NetworknetLunarCoins = newNetLunarCoins;
	}

	public CharacterBody GetCurrentBody()
	{
		CharacterMaster characterMaster = master;
		if ((bool)characterMaster)
		{
			return characterMaster.GetBody();
		}
		return null;
	}

	[Server]
	public void CopyLoadoutFromMaster()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.NetworkUser::CopyLoadoutFromMaster()' called on client");
		}
		else
		{
			networkLoadout.SetLoadout(master.loadout);
		}
	}

	[Server]
	public void CopyLoadoutToMaster()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.NetworkUser::CopyLoadoutToMaster()' called on client");
			return;
		}
		Loadout loadout = Loadout.RequestInstance();
		networkLoadout.CopyLoadout(loadout);
		master.SetLoadoutServer(loadout);
		Loadout.ReturnInstance(loadout);
	}

	private void SetBodyPreference(BodyIndex newBodyIndexPreference)
	{
		NetworkbodyIndexPreference = newBodyIndexPreference;
		NetworkUser.onNetworkUserBodyPreferenceChanged?.Invoke(this);
	}

	[Command]
	public void CmdSetBodyPreference(BodyIndex newBodyIndexPreference)
	{
		SetBodyPreference(newBodyIndexPreference);
	}

	[Client]
	public void SetSurvivorPreferenceClient(SurvivorDef survivorDef)
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void RoR2.NetworkUser::SetSurvivorPreferenceClient(RoR2.SurvivorDef)' called on server");
			return;
		}
		if (!survivorDef)
		{
			throw new ArgumentException("Provided object is null or invalid", "survivorDef");
		}
		BodyIndex bodyIndexFromSurvivorIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(survivorDef ? survivorDef.survivorIndex : SurvivorIndex.None);
		CallCmdSetBodyPreference(bodyIndexFromSurvivorIndex);
	}

	public SurvivorDef GetSurvivorPreference()
	{
		return SurvivorCatalog.GetSurvivorDef(SurvivorCatalog.GetSurvivorIndexFromBodyIndex(bodyIndexPreference));
	}

	private void Update()
	{
		if (localUser == null)
		{
			return;
		}
		if (Time.timeScale != 0f)
		{
			secondAccumulator += Time.unscaledDeltaTime;
		}
		if (!(secondAccumulator >= 1f))
		{
			return;
		}
		secondAccumulator -= 1f;
		if (!Run.instance)
		{
			return;
		}
		localUser.userProfile.totalRunSeconds++;
		if ((bool)masterObject)
		{
			CharacterMaster component = masterObject.GetComponent<CharacterMaster>();
			if ((bool)component && component.hasBody)
			{
				localUser.userProfile.totalAliveSeconds++;
			}
		}
	}

	public void UpdateUserName()
	{
		userName = GetNetworkPlayerName().GetResolvedName();
	}

	[Command]
	public void CmdSendConsoleCommand(string commandName, string[] args)
	{
		Console.instance.RunClientCmd(this, commandName, args);
	}

	[Client]
	public void SendServerUnlockables()
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void RoR2.NetworkUser::SendServerUnlockables()' called on server");
		}
		else if (localUser != null)
		{
			int unlockableCount = localUser.userProfile.statSheet.GetUnlockableCount();
			UnlockableIndex[] array = new UnlockableIndex[unlockableCount];
			for (int i = 0; i < unlockableCount; i++)
			{
				array[i] = localUser.userProfile.statSheet.GetUnlockableIndex(i);
			}
			CallCmdSendNewUnlockables(array);
		}
	}

	[Command]
	private void CmdSendNewUnlockables(UnlockableIndex[] newUnlockableIndices)
	{
		unlockables.Clear();
		debugUnlockablesList.Clear();
		int i = 0;
		for (int num = newUnlockableIndices.Length; i < num; i++)
		{
			UnlockableDef unlockableDef = UnlockableCatalog.GetUnlockableDef(newUnlockableIndices[i]);
			if (unlockableDef != null)
			{
				unlockables.Add(unlockableDef);
				debugUnlockablesList.Add(unlockableDef.cachedName);
			}
		}
		NetworkUser.onNetworkUserUnlockablesUpdated?.Invoke(this);
	}

	[Server]
	public void ServerRequestUnlockables()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.NetworkUser::ServerRequestUnlockables()' called on client");
		}
		else
		{
			CallRpcRequestUnlockables();
		}
	}

	[ClientRpc]
	private void RpcRequestUnlockables()
	{
		if (Util.HasEffectiveAuthority(base.gameObject))
		{
			SendServerUnlockables();
		}
	}

	[Command]
	public void CmdReportAchievement(string achievementNameToken)
	{
		Chat.SubjectFormatChatMessage subjectFormatChatMessage = new Chat.SubjectFormatChatMessage();
		subjectFormatChatMessage.baseToken = "ACHIEVEMENT_UNLOCKED_MESSAGE";
		subjectFormatChatMessage.subjectAsNetworkUser = this;
		subjectFormatChatMessage.paramTokens = new string[1] { achievementNameToken };
		Chat.SendBroadcastChat(subjectFormatChatMessage);
	}

	[Command]
	public void CmdReportUnlock(UnlockableIndex unlockIndex)
	{
		Debug.LogFormat("NetworkUser.CmdReportUnlock({0})", unlockIndex);
		UnlockableDef unlockableDef = UnlockableCatalog.GetUnlockableDef(unlockIndex);
		if (unlockableDef != null)
		{
			ServerHandleUnlock(unlockableDef);
		}
	}

	[Server]
	public void ServerHandleUnlock([NotNull] UnlockableDef unlockableDef)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.NetworkUser::ServerHandleUnlock(RoR2.UnlockableDef)' called on client");
			return;
		}
		Debug.LogFormat("NetworkUser.ServerHandleUnlock({0})", unlockableDef.cachedName);
		if ((bool)masterObject)
		{
			PlayerStatsComponent component = masterObject.GetComponent<PlayerStatsComponent>();
			if ((bool)component)
			{
				component.currentStats.AddUnlockable(unlockableDef);
				component.ForceNextTransmit();
			}
		}
	}

	[Server]
	public void ServerRemoveUnlock(UnlockableIndex index)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.NetworkUser::ServerRemoveUnlock(RoR2.UnlockableIndex)' called on client");
			return;
		}
		UnlockableDef unlockableDef = UnlockableCatalog.GetUnlockableDef(index);
		Debug.LogFormat("NetworkUser.ServerRemoveUnlock({0})", unlockableDef.cachedName);
		if ((bool)masterObject)
		{
			PlayerStatsComponent component = masterObject.GetComponent<PlayerStatsComponent>();
			if ((bool)component)
			{
				component.currentStats.RemoveUnlockable(index);
				component.ForceNextTransmit();
			}
		}
		(localUser?.userProfile)?.RevokeUnlockable(unlockableDef);
	}

	[Command]
	public void CmdAcknowledgeStage(NetworkInstanceId stageNetworkId)
	{
		serverLastStageAcknowledgedByClient = stageNetworkId;
	}

	[Command]
	public void CmdSubmitVote(GameObject voteControllerGameObject, int choiceIndex)
	{
		if ((bool)voteControllerGameObject)
		{
			VoteController component = voteControllerGameObject.GetComponent<VoteController>();
			if ((bool)component)
			{
				component.ReceiveUserVote(this, choiceIndex);
			}
		}
	}

	public static bool AllParticipatingNetworkUsersReady()
	{
		ReadOnlyCollection<NetworkUser> readOnlyCollection = readOnlyInstancesList;
		for (int i = 0; i < readOnlyCollection.Count; i++)
		{
			NetworkUser networkUser = readOnlyCollection[i];
			if (networkUser.isParticipating && !networkUser.connectionToClient.isReady)
			{
				return false;
			}
		}
		return true;
	}

	[CanBeNull]
	private static NetworkUser FindNetworkUserByUserProfile([NotNull] UserProfile userProfile)
	{
		if (userProfile == null)
		{
			throw new ArgumentNullException("userProfile");
		}
		ReadOnlyCollection<LocalUser> readOnlyLocalUsersList = LocalUserManager.readOnlyLocalUsersList;
		for (int i = 0; i < readOnlyLocalUsersList.Count; i++)
		{
			LocalUser localUser = readOnlyLocalUsersList[i];
			if (localUser.userProfile == userProfile)
			{
				return localUser.currentNetworkUser;
			}
		}
		return null;
	}

	static NetworkUser()
	{
		instancesList = new List<NetworkUser>();
		readOnlyInstancesList = new ReadOnlyCollection<NetworkUser>(instancesList);
		localPlayers = new List<NetworkUser>();
		readOnlyLocalPlayersList = new ReadOnlyCollection<NetworkUser>(localPlayers);
		kCmdCmdAwardLunarCoins = 654823536;
		NetworkBehaviour.RegisterCommandDelegate(typeof(NetworkUser), kCmdCmdAwardLunarCoins, InvokeCmdCmdAwardLunarCoins);
		kCmdCmdStoreRebirthItems = -2063320580;
		NetworkBehaviour.RegisterCommandDelegate(typeof(NetworkUser), kCmdCmdStoreRebirthItems, InvokeCmdCmdStoreRebirthItems);
		kCmdCmdSetRebirthItems = 1883408829;
		NetworkBehaviour.RegisterCommandDelegate(typeof(NetworkUser), kCmdCmdSetRebirthItems, InvokeCmdCmdSetRebirthItems);
		kCmdCmdSetNetLunarCoins = -934763456;
		NetworkBehaviour.RegisterCommandDelegate(typeof(NetworkUser), kCmdCmdSetNetLunarCoins, InvokeCmdCmdSetNetLunarCoins);
		kCmdCmdSetBodyPreference = 234442470;
		NetworkBehaviour.RegisterCommandDelegate(typeof(NetworkUser), kCmdCmdSetBodyPreference, InvokeCmdCmdSetBodyPreference);
		kCmdCmdSendConsoleCommand = -1997680971;
		NetworkBehaviour.RegisterCommandDelegate(typeof(NetworkUser), kCmdCmdSendConsoleCommand, InvokeCmdCmdSendConsoleCommand);
		kCmdCmdSendNewUnlockables = 1855027350;
		NetworkBehaviour.RegisterCommandDelegate(typeof(NetworkUser), kCmdCmdSendNewUnlockables, InvokeCmdCmdSendNewUnlockables);
		kCmdCmdReportAchievement = -1674656990;
		NetworkBehaviour.RegisterCommandDelegate(typeof(NetworkUser), kCmdCmdReportAchievement, InvokeCmdCmdReportAchievement);
		kCmdCmdReportUnlock = -1831223439;
		NetworkBehaviour.RegisterCommandDelegate(typeof(NetworkUser), kCmdCmdReportUnlock, InvokeCmdCmdReportUnlock);
		kCmdCmdAcknowledgeStage = -2118585573;
		NetworkBehaviour.RegisterCommandDelegate(typeof(NetworkUser), kCmdCmdAcknowledgeStage, InvokeCmdCmdAcknowledgeStage);
		kCmdCmdSubmitVote = 329593659;
		NetworkBehaviour.RegisterCommandDelegate(typeof(NetworkUser), kCmdCmdSubmitVote, InvokeCmdCmdSubmitVote);
		kRpcRpcDeductLunarCoins = -1554352898;
		NetworkBehaviour.RegisterRpcDelegate(typeof(NetworkUser), kRpcRpcDeductLunarCoins, InvokeRpcRpcDeductLunarCoins);
		kRpcRpcStoreRebirthItems = -669811482;
		NetworkBehaviour.RegisterRpcDelegate(typeof(NetworkUser), kRpcRpcStoreRebirthItems, InvokeRpcRpcStoreRebirthItems);
		kRpcRpcAwardLunarCoins = -604060198;
		NetworkBehaviour.RegisterRpcDelegate(typeof(NetworkUser), kRpcRpcAwardLunarCoins, InvokeRpcRpcAwardLunarCoins);
		kRpcRpcRequestUnlockables = -1809653515;
		NetworkBehaviour.RegisterRpcDelegate(typeof(NetworkUser), kRpcRpcRequestUnlockables, InvokeRpcRpcRequestUnlockables);
		NetworkCRC.RegisterBehaviour("NetworkUser", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdAwardLunarCoins(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdAwardLunarCoins called on client.");
		}
		else
		{
			((NetworkUser)obj).CmdAwardLunarCoins(reader.ReadPackedUInt32());
		}
	}

	protected static void InvokeCmdCmdStoreRebirthItems(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdStoreRebirthItems called on client.");
		}
		else
		{
			((NetworkUser)obj).CmdStoreRebirthItems((ItemIndex)reader.ReadInt32());
		}
	}

	protected static void InvokeCmdCmdSetRebirthItems(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetRebirthItems called on client.");
		}
		else
		{
			((NetworkUser)obj).CmdSetRebirthItems((ItemIndex)reader.ReadInt32());
		}
	}

	protected static void InvokeCmdCmdSetNetLunarCoins(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetNetLunarCoins called on client.");
		}
		else
		{
			((NetworkUser)obj).CmdSetNetLunarCoins(reader.ReadPackedUInt32());
		}
	}

	protected static void InvokeCmdCmdSetBodyPreference(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetBodyPreference called on client.");
		}
		else
		{
			((NetworkUser)obj).CmdSetBodyPreference((BodyIndex)reader.ReadInt32());
		}
	}

	protected static void InvokeCmdCmdSendConsoleCommand(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSendConsoleCommand called on client.");
		}
		else
		{
			((NetworkUser)obj).CmdSendConsoleCommand(reader.ReadString(), GeneratedNetworkCode._ReadArrayString_None(reader));
		}
	}

	protected static void InvokeCmdCmdSendNewUnlockables(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSendNewUnlockables called on client.");
		}
		else
		{
			((NetworkUser)obj).CmdSendNewUnlockables(GeneratedNetworkCode._ReadArrayUnlockableIndex_None(reader));
		}
	}

	protected static void InvokeCmdCmdReportAchievement(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdReportAchievement called on client.");
		}
		else
		{
			((NetworkUser)obj).CmdReportAchievement(reader.ReadString());
		}
	}

	protected static void InvokeCmdCmdReportUnlock(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdReportUnlock called on client.");
		}
		else
		{
			((NetworkUser)obj).CmdReportUnlock((UnlockableIndex)reader.ReadInt32());
		}
	}

	protected static void InvokeCmdCmdAcknowledgeStage(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdAcknowledgeStage called on client.");
		}
		else
		{
			((NetworkUser)obj).CmdAcknowledgeStage(reader.ReadNetworkId());
		}
	}

	protected static void InvokeCmdCmdSubmitVote(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSubmitVote called on client.");
		}
		else
		{
			((NetworkUser)obj).CmdSubmitVote(reader.ReadGameObject(), (int)reader.ReadPackedUInt32());
		}
	}

	public void CallCmdAwardLunarCoins(uint count)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdAwardLunarCoins called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdAwardLunarCoins(count);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdAwardLunarCoins);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32(count);
		SendCommandInternal(networkWriter, 0, "CmdAwardLunarCoins");
	}

	public void CallCmdStoreRebirthItems(ItemIndex item)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdStoreRebirthItems called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdStoreRebirthItems(item);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdStoreRebirthItems);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write((int)item);
		SendCommandInternal(networkWriter, 0, "CmdStoreRebirthItems");
	}

	public void CallCmdSetRebirthItems(ItemIndex item)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSetRebirthItems called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSetRebirthItems(item);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSetRebirthItems);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write((int)item);
		SendCommandInternal(networkWriter, 0, "CmdSetRebirthItems");
	}

	public void CallCmdSetNetLunarCoins(uint newNetLunarCoins)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSetNetLunarCoins called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSetNetLunarCoins(newNetLunarCoins);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSetNetLunarCoins);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32(newNetLunarCoins);
		SendCommandInternal(networkWriter, 0, "CmdSetNetLunarCoins");
	}

	public void CallCmdSetBodyPreference(BodyIndex newBodyIndexPreference)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSetBodyPreference called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSetBodyPreference(newBodyIndexPreference);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSetBodyPreference);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write((int)newBodyIndexPreference);
		SendCommandInternal(networkWriter, 0, "CmdSetBodyPreference");
	}

	public void CallCmdSendConsoleCommand(string commandName, string[] args)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSendConsoleCommand called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSendConsoleCommand(commandName, args);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSendConsoleCommand);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(commandName);
		GeneratedNetworkCode._WriteArrayString_None(networkWriter, args);
		SendCommandInternal(networkWriter, 0, "CmdSendConsoleCommand");
	}

	public void CallCmdSendNewUnlockables(UnlockableIndex[] newUnlockableIndices)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSendNewUnlockables called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSendNewUnlockables(newUnlockableIndices);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSendNewUnlockables);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WriteArrayUnlockableIndex_None(networkWriter, newUnlockableIndices);
		SendCommandInternal(networkWriter, 0, "CmdSendNewUnlockables");
	}

	public void CallCmdReportAchievement(string achievementNameToken)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdReportAchievement called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdReportAchievement(achievementNameToken);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdReportAchievement);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(achievementNameToken);
		SendCommandInternal(networkWriter, 0, "CmdReportAchievement");
	}

	public void CallCmdReportUnlock(UnlockableIndex unlockIndex)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdReportUnlock called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdReportUnlock(unlockIndex);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdReportUnlock);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write((int)unlockIndex);
		SendCommandInternal(networkWriter, 0, "CmdReportUnlock");
	}

	public void CallCmdAcknowledgeStage(NetworkInstanceId stageNetworkId)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdAcknowledgeStage called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdAcknowledgeStage(stageNetworkId);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdAcknowledgeStage);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(stageNetworkId);
		SendCommandInternal(networkWriter, 0, "CmdAcknowledgeStage");
	}

	public void CallCmdSubmitVote(GameObject voteControllerGameObject, int choiceIndex)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSubmitVote called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSubmitVote(voteControllerGameObject, choiceIndex);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSubmitVote);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(voteControllerGameObject);
		networkWriter.WritePackedUInt32((uint)choiceIndex);
		SendCommandInternal(networkWriter, 0, "CmdSubmitVote");
	}

	protected static void InvokeRpcRpcDeductLunarCoins(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcDeductLunarCoins called on server.");
		}
		else
		{
			((NetworkUser)obj).RpcDeductLunarCoins(reader.ReadPackedUInt32());
		}
	}

	protected static void InvokeRpcRpcStoreRebirthItems(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcStoreRebirthItems called on server.");
		}
		else
		{
			((NetworkUser)obj).RpcStoreRebirthItems((ItemIndex)reader.ReadInt32());
		}
	}

	protected static void InvokeRpcRpcAwardLunarCoins(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcAwardLunarCoins called on server.");
		}
		else
		{
			((NetworkUser)obj).RpcAwardLunarCoins(reader.ReadPackedUInt32());
		}
	}

	protected static void InvokeRpcRpcRequestUnlockables(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcRequestUnlockables called on server.");
		}
		else
		{
			((NetworkUser)obj).RpcRequestUnlockables();
		}
	}

	public void CallRpcDeductLunarCoins(uint count)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcDeductLunarCoins called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcDeductLunarCoins);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32(count);
		SendRPCInternal(networkWriter, 0, "RpcDeductLunarCoins");
	}

	public void CallRpcStoreRebirthItems(ItemIndex item)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcStoreRebirthItems called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcStoreRebirthItems);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write((int)item);
		SendRPCInternal(networkWriter, 0, "RpcStoreRebirthItems");
	}

	public void CallRpcAwardLunarCoins(uint count)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcAwardLunarCoins called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcAwardLunarCoins);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32(count);
		SendRPCInternal(networkWriter, 0, "RpcAwardLunarCoins");
	}

	public void CallRpcRequestUnlockables()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcRequestUnlockables called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcRequestUnlockables);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcRequestUnlockables");
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			GeneratedNetworkCode._WriteNetworkUserId_None(writer, _id);
			writer.WritePackedUInt32(rewiredPlayerId);
			writer.Write(_masterObjectId);
			writer.Write(userColor);
			writer.WritePackedUInt32(netLunarCoins);
			writer.Write((int)rebirthItem);
			writer.Write((int)bodyIndexPreference);
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
			GeneratedNetworkCode._WriteNetworkUserId_None(writer, _id);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32(rewiredPlayerId);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_masterObjectId);
		}
		if ((base.syncVarDirtyBits & 8u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(userColor);
		}
		if ((base.syncVarDirtyBits & 0x10u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32(netLunarCoins);
		}
		if ((base.syncVarDirtyBits & 0x20u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write((int)rebirthItem);
		}
		if ((base.syncVarDirtyBits & 0x40u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write((int)bodyIndexPreference);
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
			_id = GeneratedNetworkCode._ReadNetworkUserId_None(reader);
			rewiredPlayerId = (byte)reader.ReadPackedUInt32();
			_masterObjectId = reader.ReadNetworkId();
			userColor = reader.ReadColor32();
			netLunarCoins = reader.ReadPackedUInt32();
			rebirthItem = (ItemIndex)reader.ReadInt32();
			bodyIndexPreference = (BodyIndex)reader.ReadInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			OnSyncId(GeneratedNetworkCode._ReadNetworkUserId_None(reader));
		}
		if (((uint)num & 2u) != 0)
		{
			rewiredPlayerId = (byte)reader.ReadPackedUInt32();
		}
		if (((uint)num & 4u) != 0)
		{
			OnSyncMasterObjectId(reader.ReadNetworkId());
		}
		if (((uint)num & 8u) != 0)
		{
			userColor = reader.ReadColor32();
		}
		if (((uint)num & 0x10u) != 0)
		{
			netLunarCoins = reader.ReadPackedUInt32();
		}
		if (((uint)num & 0x20u) != 0)
		{
			rebirthItem = (ItemIndex)reader.ReadInt32();
		}
		if (((uint)num & 0x40u) != 0)
		{
			SetBodyPreference((BodyIndex)reader.ReadInt32());
		}
	}

	public override void PreStartClient()
	{
	}
}

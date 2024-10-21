using System;
using System.Collections.Generic;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.UI;
using Facepunch.Steamworks;
using HG;
using RoR2.Networking;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class EOSLobbyManager : PCLobbyManager
{
	public class Filter
	{
		public List<AttributeData> SearchData = new List<AttributeData>();
	}

	private struct LobbyRefreshRequest
	{
		public object requester;

		public Filter filter;

		public Action<List<LobbyDetails>> callback;
	}

	private const MPFeatures PlatformFeatureFlags = MPFeatures.HostGame | MPFeatures.FindGame;

	private const MPLobbyFeatures PlatformLobbyUIFlags = MPLobbyFeatures.CreateLobby | MPLobbyFeatures.SocialIcon | MPLobbyFeatures.HostPromotion | MPLobbyFeatures.Clipboard | MPLobbyFeatures.Invite | MPLobbyFeatures.UserIcon | MPLobbyFeatures.LeaveLobby | MPLobbyFeatures.LobbyDropdownOptions;

	private bool _ownsLobby;

	private int minimumPlayerCount = 2;

	private string currentLobbyId = string.Empty;

	private LobbyDetails currentLobbyDetails;

	private LobbyModification currentLobbyModificationHandle;

	private LobbySearch currentSearchHandle;

	private LobbySearch joinClipboardLobbySearchHandle;

	private const int MaxSearchResults = 50;

	public const string mdEdition = "v";

	public const string mdAppId = "appid";

	public const string mdTotalMaxPlayers = "total_max_players";

	public const string mdPlayerCount = "player_count";

	public const string mdQuickplayQueued = "qp";

	public const string mdQuickplayCutoffTime = "qp_cutoff_time";

	public const string mdStarting = "starting";

	public const string mdBuildId = "build_id";

	public const string mdServerId = "server_id";

	public const string mdServerAddress = "server_address";

	public const string mdMap = "_map";

	public const string mdRuleBook = "rulebook";

	public const string mdMigrationId = "migration_id";

	public const string mdHasPassword = "_pw";

	public const string mdIsDedicatedServer = "_ds";

	public const string mdServerName = "_svnm";

	public const string mdServerTags = "_svtags";

	public const string mdServerMaxPlayers = "_svmpl";

	public const string mdServerPlayerCount = "_svplc";

	public const string mdGameModeName = "_svgm";

	public const string mdModHash = "_mh";

	public const string BucketName = "gbx_internal";

	private LobbyInterface lobbyInterface;

	private readonly List<int> playerCountsList = new List<int>();

	private MemoizedToString<int, ToStringImplementationInvariant> localPlayerCountToString = MemoizedToString<int, ToStringImplementationInvariant>.GetNew();

	private const string LobbyJoinIndicator = "ClipboardJoin";

	private bool startingFadeSet;

	private string lastHostingLobbyId;

	private const float quickplayCutoffTime = 30f;

	private Queue<LobbyRefreshRequest> lobbyRefreshRequests = new Queue<LobbyRefreshRequest>();

	private LobbyRefreshRequest? currentRefreshRequest;

	private bool hostingServer;

	private PlatformID currentServerId;

	private static readonly char[] charactersToCheck = "abcdef".ToCharArray();

	public override bool isInLobby
	{
		get
		{
			return currentLobbyId != string.Empty;
		}
		protected set
		{
		}
	}

	public override bool ownsLobby
	{
		get
		{
			return _ownsLobby;
		}
		protected set
		{
			if (value != _ownsLobby)
			{
				_ownsLobby = value;
				if (_ownsLobby)
				{
					OnLobbyOwnershipGained();
					UpdatePlayerCount();
				}
				else
				{
					OnLobbyOwnershipLost();
				}
			}
		}
	}

	public override bool hasMinimumPlayerCount => newestLobbyData.totalPlayerCount >= minimumPlayerCount;

	public int remoteMachineCount { get; private set; }

	public string CurrentLobbyId
	{
		get
		{
			return currentLobbyId;
		}
		private set
		{
		}
	}

	public LobbyDetails CurrentLobbyDetails
	{
		get
		{
			return currentLobbyDetails;
		}
		private set
		{
		}
	}

	public LobbyModification CurrentLobbyModification
	{
		get
		{
			return currentLobbyModificationHandle;
		}
		private set
		{
		}
	}

	public PlatformID serverId => newestLobbyData.serverId;

	public override LobbyData newestLobbyData { get; protected set; }

	public override int calculatedTotalPlayerCount { get; protected set; }

	public override int calculatedExtraPlayersCount { get; protected set; }

	public override LobbyType currentLobbyType
	{
		get
		{
			if (currentLobbyDetails != null)
			{
				LobbyDetailsCopyInfoOptions options = default(LobbyDetailsCopyInfoOptions);
				currentLobbyDetails.CopyInfo(ref options, out var outLobbyDetailsInfo);
				return PermissionLevelToType(outLobbyDetailsInfo.Value.PermissionLevel);
			}
			return LobbyType.Error;
		}
		set
		{
		}
	}

	public override bool IsBusy { get; set; }

	public override MPFeatures GetPlatformMPFeatureFlags()
	{
		return MPFeatures.HostGame | MPFeatures.FindGame;
	}

	public override MPLobbyFeatures GetPlatformMPLobbyFeatureFlags()
	{
		return MPLobbyFeatures.CreateLobby | MPLobbyFeatures.SocialIcon | MPLobbyFeatures.HostPromotion | MPLobbyFeatures.Clipboard | MPLobbyFeatures.Invite | MPLobbyFeatures.UserIcon | MPLobbyFeatures.LeaveLobby | MPLobbyFeatures.LobbyDropdownOptions;
	}

	public static EOSLobbyManager GetFromPlatformSystems()
	{
		return PlatformSystems.lobbyManager as EOSLobbyManager;
	}

	private void UpdateOwnsLobby()
	{
		LobbyDetailsGetLobbyOwnerOptions options = default(LobbyDetailsGetLobbyOwnerOptions);
		ownsLobby = currentLobbyDetails != null && currentLobbyDetails.GetLobbyOwner(ref options) == EOSLoginManager.loggedInProductId;
	}

	public void Init()
	{
		lobbyInterface = EOSPlatformManager.GetPlatformInterface().GetLobbyInterface();
		if (lobbyInterface == null)
		{
			Debug.LogError("Unable to Obtain EOS Lobby Interface!");
		}
		SetupLobbyCallbacks();
		RoR2Application.onUpdate += StaticUpdate;
		LobbyDataSetupState setupState = new LobbyDataSetupState
		{
			totalMaxPlayers = RoR2Application.maxPlayers
		};
		newestLobbyData = new LobbyData(setupState);
		LocalUserManager.onLocalUsersUpdated += UpdatePlayerCount;
		NetworkManagerSystem.onStartServerGlobal += OnStartHostingServer;
		NetworkManagerSystem.onStopServerGlobal += OnStopHostingServer;
		NetworkManagerSystem.onStopClientGlobal += OnStopClient;
		NetworkManagerSystem.onStopClientGlobal += delegate
		{
			SetStartingIfOwner(startingState: false);
		};
		onLobbyOwnershipGained = (Action)Delegate.Combine(onLobbyOwnershipGained, (Action)delegate
		{
			SetStartingIfOwner(startingState: false);
		});
		UserManager.OnDisplayNameMappingComplete += UpdateLobbyNames;
		SetStartingIfOwner(startingState: false);
		SteamworksClientManager.onLoaded += SetSteamLobbyCallbacks;
	}

	public override void Shutdown()
	{
		base.Shutdown();
		lobbyInterface = null;
	}

	private void SetSteamLobbyCallbacks()
	{
		Client steamworksClient = SteamworksClientManager.instance.steamworksClient;
		steamworksClient.Lobby.OnUserInvitedToLobby = OnUserInvitedToSteamLobby;
		steamworksClient.Lobby.OnLobbyJoinRequested = OnSteamLobbyJoinRequested;
	}

	private void OnSteamLobbyJoinRequested(ulong lobbyId)
	{
		Debug.LogFormat("Request to join lobby {0} received but we're in cross-play, rejecting", lobbyId);
		PCLobbyManager.ShowEnableCrossPlayPopup(isLobbyCrossplay: false);
	}

	private void OnUserInvitedToSteamLobby(ulong lobbyId, ulong senderId)
	{
		Debug.LogFormat("Received invitation to lobby {0} from sender {1} but we're in cross play, rejecting", lobbyId, senderId);
	}

	private void UpdateLobbyNames()
	{
		onLobbyDataUpdated?.Invoke();
	}

	private void SetupLobbyCallbacks()
	{
		AddNotifyLobbyUpdateReceivedOptions options = default(AddNotifyLobbyUpdateReceivedOptions);
		lobbyInterface.AddNotifyLobbyUpdateReceived(ref options, null, OnLobbyDataUpdateReceived);
		AddNotifyJoinLobbyAcceptedOptions options2 = default(AddNotifyJoinLobbyAcceptedOptions);
		lobbyInterface.AddNotifyJoinLobbyAccepted(ref options2, null, OnJoinLobbyAccepted);
		AddNotifyLobbyInviteReceivedOptions options3 = default(AddNotifyLobbyInviteReceivedOptions);
		lobbyInterface.AddNotifyLobbyInviteReceived(ref options3, null, OnUserInvitedToLobby);
		AddNotifyLobbyInviteAcceptedOptions options4 = default(AddNotifyLobbyInviteAcceptedOptions);
		lobbyInterface.AddNotifyLobbyInviteAccepted(ref options4, null, OnLobbyInviteAccepted);
	}

	public override int GetLobbyMemberPlayerCountByIndex(int memberIndex)
	{
		if (memberIndex >= playerCountsList.Count)
		{
			return 0;
		}
		return playerCountsList[memberIndex];
	}

	public override void CheckIfInitializedAndValid()
	{
	}

	private void UpdateMemberAttribute(string inKey, string inValue)
	{
		if (CurrentLobbyModification != null)
		{
			Debug.LogFormat("Setting lobby member attribute {0} to value {1} for user {2}", inKey, inValue, EOSLoginManager.loggedInProductId);
			LobbyModificationRemoveMemberAttributeOptions lobbyModificationRemoveMemberAttributeOptions = default(LobbyModificationRemoveMemberAttributeOptions);
			lobbyModificationRemoveMemberAttributeOptions.Key = inKey;
			LobbyModificationRemoveMemberAttributeOptions options = lobbyModificationRemoveMemberAttributeOptions;
			CurrentLobbyModification.RemoveMemberAttribute(ref options);
			LobbyModificationAddMemberAttributeOptions lobbyModificationAddMemberAttributeOptions = default(LobbyModificationAddMemberAttributeOptions);
			lobbyModificationAddMemberAttributeOptions.Attribute = new AttributeData
			{
				Key = inKey,
				Value = inValue
			};
			lobbyModificationAddMemberAttributeOptions.Visibility = LobbyAttributeVisibility.Public;
			LobbyModificationAddMemberAttributeOptions options2 = lobbyModificationAddMemberAttributeOptions;
			CurrentLobbyModification.AddMemberAttribute(ref options2);
			UpdateLobby(CurrentLobbyModification);
		}
	}

	private void UpdatePlayerCount()
	{
		if (currentLobbyDetails != null)
		{
			int count = LocalUserManager.readOnlyLocalUsersList.Count;
			string @string = localPlayerCountToString.GetString(Math.Max(1, count));
			LobbyDetailsCopyMemberAttributeByKeyOptions lobbyDetailsCopyMemberAttributeByKeyOptions = default(LobbyDetailsCopyMemberAttributeByKeyOptions);
			lobbyDetailsCopyMemberAttributeByKeyOptions.AttrKey = "player_count";
			lobbyDetailsCopyMemberAttributeByKeyOptions.TargetUserId = EOSLoginManager.loggedInProductId;
			LobbyDetailsCopyMemberAttributeByKeyOptions options = lobbyDetailsCopyMemberAttributeByKeyOptions;
			if (CurrentLobbyDetails.CopyMemberAttributeByKey(ref options, out var outAttribute) == Result.Success)
			{
				if ((string)outAttribute.Value.Data.Value.Value.AsUtf8 != @string && CurrentLobbyModification != null)
				{
					UpdateMemberAttribute("player_count", @string);
				}
			}
			else
			{
				UpdateMemberAttribute("player_count", @string);
			}
			playerCountsList.Clear();
			calculatedTotalPlayerCount = 0;
			remoteMachineCount = 0;
			calculatedExtraPlayersCount = 0;
			ProductUserId loggedInProductId = EOSLoginManager.loggedInProductId;
			LobbyDetailsGetMemberCountOptions options2 = default(LobbyDetailsGetMemberCountOptions);
			uint memberCount = currentLobbyDetails.GetMemberCount(ref options2);
			for (uint num = 0u; num < memberCount; num++)
			{
				LobbyDetailsGetMemberByIndexOptions lobbyDetailsGetMemberByIndexOptions = default(LobbyDetailsGetMemberByIndexOptions);
				lobbyDetailsGetMemberByIndexOptions.MemberIndex = num;
				LobbyDetailsGetMemberByIndexOptions options3 = lobbyDetailsGetMemberByIndexOptions;
				ProductUserId memberByIndex = currentLobbyDetails.GetMemberByIndex(ref options3);
				int result = 1;
				LobbyDetailsCopyMemberAttributeByKeyOptions options4 = new LobbyDetailsCopyMemberAttributeByKeyOptions
				{
					AttrKey = "player_count",
					TargetUserId = EOSLoginManager.loggedInProductId
				};
				if (CurrentLobbyDetails.CopyMemberAttributeByKey(ref options4, out var outAttribute2) == Result.Success)
				{
					result = ((!TextSerialization.TryParseInvariant((string)outAttribute2.Value.Data.Value.Value.AsUtf8, out result)) ? 1 : Math.Max(1, result));
				}
				if (memberByIndex == loggedInProductId)
				{
					result = Math.Max(1, count);
				}
				else
				{
					int num2 = remoteMachineCount + 1;
					remoteMachineCount = num2;
				}
				playerCountsList.Add(result);
				calculatedTotalPlayerCount += result;
				if (result > 1)
				{
					calculatedExtraPlayersCount += result - 1;
				}
			}
		}
		onPlayerCountUpdated?.Invoke();
	}

	private void OnLobbyChanged()
	{
		OnLobbyDataUpdated();
		onLobbyChanged?.Invoke();
		(PlatformSystems.userManager as UserManagerEOS).QueryForDisplayNames(GetLobbyMembers());
	}

	public override void CreateLobby()
	{
		CreateLobbyOptions createLobbyOptions = default(CreateLobbyOptions);
		createLobbyOptions.LocalUserId = EOSLoginManager.loggedInProductId;
		createLobbyOptions.MaxLobbyMembers = (uint)RoR2Application.maxPlayers;
		createLobbyOptions.PermissionLevel = ((preferredLobbyType != LobbyType.Public) ? LobbyPermissionLevel.Joinviapresence : LobbyPermissionLevel.Publicadvertised);
		createLobbyOptions.BucketId = "gbx_internal";
		createLobbyOptions.PresenceEnabled = true;
		createLobbyOptions.AllowInvites = true;
		CreateLobbyOptions options = createLobbyOptions;
		base.awaitingCreate = true;
		lobbyInterface?.CreateLobby(ref options, null, OnLobbyCreated);
	}

	public override void JoinLobby(PlatformID uid)
	{
		LeaveLobby(delegate
		{
			JoinLobbyOptions joinLobbyOptions = default(JoinLobbyOptions);
			joinLobbyOptions.LocalUserId = EOSLoginManager.loggedInProductId;
			JoinLobbyOptions options = joinLobbyOptions;
			lobbyInterface?.JoinLobby(ref options, null, OnLobbyJoined);
		});
		base.awaitingJoin = true;
	}

	public void JoinLobby(LobbyDetails lobbyID)
	{
		LeaveLobby(delegate
		{
			JoinLobbyOptions joinLobbyOptions = default(JoinLobbyOptions);
			joinLobbyOptions.LocalUserId = EOSLoginManager.loggedInProductId;
			joinLobbyOptions.LobbyDetailsHandle = lobbyID;
			JoinLobbyOptions options = joinLobbyOptions;
			lobbyInterface?.JoinLobby(ref options, null, OnLobbyJoined);
		});
		base.awaitingJoin = true;
	}

	public void FindClipboardLobby(string lobbyId)
	{
		if (joinClipboardLobbySearchHandle != null)
		{
			joinClipboardLobbySearchHandle.Release();
			joinClipboardLobbySearchHandle = null;
		}
		joinClipboardLobbySearchHandle = new LobbySearch();
		CreateLobbySearchOptions createLobbySearchOptions = default(CreateLobbySearchOptions);
		createLobbySearchOptions.MaxResults = 1u;
		CreateLobbySearchOptions options = createLobbySearchOptions;
		if (lobbyInterface?.CreateLobbySearch(ref options, out joinClipboardLobbySearchHandle) == Result.Success)
		{
			LobbySearchSetLobbyIdOptions lobbySearchSetLobbyIdOptions = default(LobbySearchSetLobbyIdOptions);
			lobbySearchSetLobbyIdOptions.LobbyId = lobbyId;
			LobbySearchSetLobbyIdOptions options2 = lobbySearchSetLobbyIdOptions;
			joinClipboardLobbySearchHandle.SetLobbyId(ref options2);
			LobbySearchFindOptions lobbySearchFindOptions = default(LobbySearchFindOptions);
			lobbySearchFindOptions.LocalUserId = EOSLoginManager.loggedInProductId;
			LobbySearchFindOptions options3 = lobbySearchFindOptions;
			joinClipboardLobbySearchHandle.Find(ref options3, "ClipboardJoin", OnLobbySearchComplete);
		}
	}

	public override void LeaveLobby()
	{
		LeaveLobbyOptions leaveLobbyOptions = default(LeaveLobbyOptions);
		leaveLobbyOptions.LocalUserId = EOSLoginManager.loggedInProductId;
		leaveLobbyOptions.LobbyId = currentLobbyId;
		LeaveLobbyOptions options = leaveLobbyOptions;
		lobbyInterface?.LeaveLobby(ref options, null, OnLobbyLeave);
		currentLobbyId = string.Empty;
	}

	private void LeaveLobby(Action callback)
	{
		LeaveLobbyOptions leaveLobbyOptions = default(LeaveLobbyOptions);
		leaveLobbyOptions.LocalUserId = EOSLoginManager.loggedInProductId;
		leaveLobbyOptions.LobbyId = currentLobbyId;
		LeaveLobbyOptions options = leaveLobbyOptions;
		lobbyInterface?.LeaveLobby(ref options, null, delegate(ref LeaveLobbyCallbackInfo data)
		{
			OnLobbyLeave(ref data);
			callback?.Invoke();
		});
		currentLobbyId = string.Empty;
	}

	public override PlatformID[] GetLobbyMembers()
	{
		LobbyDetailsGetMemberCountOptions options = default(LobbyDetailsGetMemberCountOptions);
		uint? num = CurrentLobbyDetails?.GetMemberCount(ref options);
		PlatformID[] array = null;
		if (num.HasValue)
		{
			array = new PlatformID[num.Value];
			for (uint num2 = 0u; num2 < array.Length; num2++)
			{
				LobbyDetailsGetMemberByIndexOptions lobbyDetailsGetMemberByIndexOptions = default(LobbyDetailsGetMemberByIndexOptions);
				lobbyDetailsGetMemberByIndexOptions.MemberIndex = num2;
				LobbyDetailsGetMemberByIndexOptions options2 = lobbyDetailsGetMemberByIndexOptions;
				array[num2] = new PlatformID(CurrentLobbyDetails.GetMemberByIndex(ref options2));
			}
		}
		return array;
	}

	public override bool ShouldShowPromoteButton()
	{
		return false;
	}

	private void Update()
	{
		if (startingFadeSet != (newestLobbyData.starting && !ClientScene.ready))
		{
			if (startingFadeSet)
			{
				FadeToBlackManager.fadeCount--;
			}
			else
			{
				FadeToBlackManager.fadeCount++;
			}
			startingFadeSet = !startingFadeSet;
		}
	}

	private void StaticUpdate()
	{
		UpdateOwnsLobby();
	}

	private PlatformID GetLaunchParamsLobbyId()
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length - 1; i++)
		{
			if (string.Equals(commandLineArgs[i], "+connect_lobby", StringComparison.OrdinalIgnoreCase) && PlatformID.TryParse(ArrayUtils.GetSafe(commandLineArgs, i + 1, in string.Empty), out var result))
			{
				return result;
			}
		}
		return PlatformID.nil;
	}

	public void ForceLobbyDataUpdate()
	{
	}

	public void SendLobbyMessage(LobbyMessageType messageType, NetworkWriter writer)
	{
		byte[] array = new byte[1 + writer.Position];
		array[0] = (byte)messageType;
		Array.Copy(writer.AsArray(), 0, array, 1, writer.Position);
	}

	public override void OnCutoffTimerComplete()
	{
	}

	private void OnChatMessageReceived(ulong senderId, byte[] buffer, int byteCount)
	{
		try
		{
			NetworkReader networkReader = new NetworkReader(buffer);
			if (byteCount >= 1)
			{
				LobbyMessageType lobbyMessageType = (LobbyMessageType)networkReader.ReadByte();
				Debug.LogFormat("Received Steamworks Lobby Message from {0} ({1}B). messageType={2}", senderId, byteCount, lobbyMessageType);
				switch (lobbyMessageType)
				{
				}
			}
			else
			{
				Debug.LogWarningFormat("Received SteamworksLobbyMessage from {0}, but the message was empty.");
			}
		}
		catch (Exception message)
		{
			Debug.LogError(message);
		}
	}

	private void OnLobbyCreated(ref CreateLobbyCallbackInfo data)
	{
		base.awaitingCreate = false;
		if (data.ResultCode == Result.Success)
		{
			currentLobbyId = data.LobbyId;
			TryGetLobbyDetails();
			OnLobbyChanged();
		}
	}

	protected void OnLobbyDataUpdated()
	{
		UpdateNewestLobbyData();
		UpdateOwnsLobby();
		UpdatePlayerCount();
		if (currentLobbyDetails != null && !ownsLobby)
		{
			if (newestLobbyData.serverId.isValid)
			{
				bool num = newestLobbyData.serverId == NetworkManagerSystem.singleton.serverP2PId || NetworkManagerSystem.singleton.IsConnectedToServer(newestLobbyData.serverId);
				bool flag = string.CompareOrdinal(RoR2Application.GetBuildId(), newestLobbyData.buildId) == 0;
				if (!num && flag)
				{
					NetworkManagerSystem.singleton.desiredHost = new HostDescription(newestLobbyData.serverId, HostDescription.HostType.EOS);
					lastHostingLobbyId = currentLobbyId;
				}
			}
			else if (lastHostingLobbyId == currentLobbyId)
			{
				Debug.LogFormat("Intercepting bad or out-of-order lobby update to server id.");
			}
			else
			{
				NetworkManagerSystem.singleton.desiredHost = HostDescription.none;
			}
		}
		onLobbyDataUpdated?.Invoke();
	}

	private void UpdateNewestLobbyData()
	{
		DateTime value = Util.UnixTimeStampToDateTimeUtc((uint)Util.GetCurrentUnixEpochTimeInSeconds()) + TimeSpan.FromSeconds(30.0);
		if (currentLobbyDetails != null)
		{
			LobbyDataSetupState lobbyDataSetupState = new LobbyDataSetupState
			{
				totalMaxPlayers = RoR2Application.maxPlayers,
				totalPlayerCount = calculatedTotalPlayerCount,
				quickplayQueued = false,
				starting = GetLobbyBoolValue(currentLobbyDetails, "starting"),
				buildId = GetLobbyStringValue(currentLobbyDetails, "build_id"),
				quickplayCutoffTime = value,
				shouldConnect = false,
				joinable = (calculatedTotalPlayerCount < RoR2Application.maxPlayers)
			};
			string lobbyStringValue = GetLobbyStringValue(currentLobbyDetails, "server_id");
			if (lobbyStringValue != string.Empty)
			{
				lobbyDataSetupState.serverId = new PlatformID(lobbyStringValue);
			}
			newestLobbyData = new LobbyData(lobbyDataSetupState);
		}
		else
		{
			newestLobbyData = new LobbyData();
		}
	}

	private void OnLobbyJoined(ref JoinLobbyCallbackInfo data)
	{
		base.awaitingJoin = false;
		bool flag = data.ResultCode == Result.Success;
		if (flag)
		{
			currentLobbyId = data.LobbyId;
			TryGetLobbyDetails();
			if (currentLobbyDetails != null)
			{
				string buildId = RoR2Application.GetBuildId();
				string lobbyStringValue = GetLobbyStringValue(currentLobbyDetails, "build_id");
				if (buildId != lobbyStringValue)
				{
					Debug.LogFormat("Lobby build_id mismatch, leaving lobby. Ours=\"{0}\" Theirs=\"{1}\"", buildId, lobbyStringValue);
					SimpleDialogBox simpleDialogBox = SimpleDialogBox.Create();
					simpleDialogBox.AddCancelButton(CommonLanguageTokens.ok);
					simpleDialogBox.headerToken = new SimpleDialogBox.TokenParamsPair
					{
						token = "STEAM_LOBBY_VERSION_MISMATCH_DIALOG_TITLE",
						formatParams = Array.Empty<object>()
					};
					SimpleDialogBox.TokenParamsPair descriptionToken = new SimpleDialogBox.TokenParamsPair
					{
						token = "STEAM_LOBBY_VERSION_MISMATCH_DIALOG_DESCRIPTION"
					};
					object[] formatParams = new string[2] { buildId, lobbyStringValue };
					descriptionToken.formatParams = formatParams;
					simpleDialogBox.descriptionToken = descriptionToken;
					return;
				}
			}
			Debug.LogFormat("lobby join succeeded. Lobby id = {0}", currentLobbyId);
			OnLobbyChanged();
		}
		else
		{
			SimpleDialogBox simpleDialogBox2 = SimpleDialogBox.Create();
			simpleDialogBox2.AddCancelButton(CommonLanguageTokens.ok);
			simpleDialogBox2.headerToken = new SimpleDialogBox.TokenParamsPair
			{
				token = "EOS_CANNOT_JOIN_STEAM_LOBBY_HEADER",
				formatParams = Array.Empty<object>()
			};
			simpleDialogBox2.descriptionToken = new SimpleDialogBox.TokenParamsPair
			{
				token = "KICK_REASON_SERVERFULL",
				formatParams = Array.Empty<object>()
			};
			Console.instance.SubmitCmd(null, "steam_lobby_create_if_none", recordSubmit: true);
		}
		onLobbyJoined?.Invoke(flag);
	}

	private void OnLobbyMemberDataUpdated(PlatformID memberId)
	{
		UpdateOwnsLobby();
		onLobbyMemberDataUpdated?.Invoke(memberId);
	}

	private void OnLobbyKicked(bool kickedDueToDisconnect, ulong lobbyId, ulong adminId)
	{
		Debug.LogFormat("Kicked from lobby. kickedDueToDisconnect={0} lobbyId={1} adminId={2}", kickedDueToDisconnect, lobbyId, adminId);
		OnLobbyChanged();
	}

	private void OnLobbyLeave(ref LeaveLobbyCallbackInfo data)
	{
		Debug.LogFormat("Left lobby {0}.", data.LobbyId);
		currentLobbyId = string.Empty;
		if (currentLobbyDetails != null)
		{
			currentLobbyDetails.Release();
			currentLobbyDetails = null;
		}
		if (currentLobbyModificationHandle != null)
		{
			currentLobbyModificationHandle.Release();
			currentLobbyModificationHandle = null;
		}
		OnLobbyChanged();
	}

	private void OnLobbyJoinRequested(ulong lobbyId)
	{
		Debug.LogFormat("Request to join lobby {0} received. Attempting to join lobby.", lobbyId);
	}

	private void OnUserInvitedToLobby(ref LobbyInviteReceivedCallbackInfo data)
	{
		Debug.LogFormat("Received invitation to lobby {0} from sender {1}.", data.InviteId, data.TargetUserId);
	}

	private static void OnSendInviteComplete(ref SendInviteCallbackInfo data)
	{
		_ = data.GetResultCode() == Result.Success;
	}

	private void OnLobbySearchComplete(ref LobbySearchFindCallbackInfo data)
	{
		if (data.GetResultCode() != Result.Success)
		{
			return;
		}
		bool flag = data.ClientData as string == "ClipboardJoin";
		List<LobbyDetails> list = new List<LobbyDetails>();
		LobbySearch lobbySearch = (flag ? joinClipboardLobbySearchHandle : currentSearchHandle);
		if (lobbySearch != null)
		{
			LobbySearchGetSearchResultCountOptions options = default(LobbySearchGetSearchResultCountOptions);
			uint searchResultCount = lobbySearch.GetSearchResultCount(ref options);
			for (uint num = 0u; num < searchResultCount; num++)
			{
				LobbyDetails outLobbyDetailsHandle = new LobbyDetails();
				LobbySearchCopySearchResultByIndexOptions lobbySearchCopySearchResultByIndexOptions = default(LobbySearchCopySearchResultByIndexOptions);
				lobbySearchCopySearchResultByIndexOptions.LobbyIndex = num;
				LobbySearchCopySearchResultByIndexOptions options2 = lobbySearchCopySearchResultByIndexOptions;
				if (lobbySearch.CopySearchResultByIndex(ref options2, out outLobbyDetailsHandle) == Result.Success)
				{
					if (flag)
					{
						JoinLobby(outLobbyDetailsHandle);
						joinClipboardLobbySearchHandle.Release();
						joinClipboardLobbySearchHandle = null;
						return;
					}
					list.Add(outLobbyDetailsHandle);
				}
			}
		}
		(data.ClientData as Action<List<LobbyDetails>>)(list);
	}

	private void OnLobbyDataUpdateReceived(ref LobbyUpdateReceivedCallbackInfo data)
	{
		if (data.LobbyId == (Utf8String)currentLobbyId)
		{
			TryGetLobbyDetails();
			OnLobbyChanged();
		}
	}

	private void OnJoinLobbyAccepted(ref JoinLobbyAcceptedCallbackInfo data)
	{
		Debug.LogFormat("Attempting to join from ui event {0} from local user {1}.", data.UiEventId, data.LocalUserId);
		LobbyDetails outLobbyDetailsHandle = new LobbyDetails();
		CopyLobbyDetailsHandleByUiEventIdOptions copyLobbyDetailsHandleByUiEventIdOptions = default(CopyLobbyDetailsHandleByUiEventIdOptions);
		copyLobbyDetailsHandleByUiEventIdOptions.UiEventId = data.UiEventId;
		CopyLobbyDetailsHandleByUiEventIdOptions options = copyLobbyDetailsHandleByUiEventIdOptions;
		Result? result = lobbyInterface?.CopyLobbyDetailsHandleByUiEventId(ref options, out outLobbyDetailsHandle);
		if (result == Result.Success)
		{
			JoinLobby(outLobbyDetailsHandle);
		}
		else
		{
			Debug.LogFormat("failed getting lobby details from join callback, result = " + result.ToString());
		}
	}

	private void OnLobbyInviteAccepted(ref LobbyInviteAcceptedCallbackInfo data)
	{
		Debug.LogFormat("Accepted invitation to lobby {0} from sender {1}.", data.InviteId, data.TargetUserId);
		LobbyDetails outLobbyDetailsHandle = new LobbyDetails();
		CopyLobbyDetailsHandleByInviteIdOptions copyLobbyDetailsHandleByInviteIdOptions = default(CopyLobbyDetailsHandleByInviteIdOptions);
		copyLobbyDetailsHandleByInviteIdOptions.InviteId = data.InviteId;
		CopyLobbyDetailsHandleByInviteIdOptions options = copyLobbyDetailsHandleByInviteIdOptions;
		Result? result = lobbyInterface?.CopyLobbyDetailsHandleByInviteId(ref options, out outLobbyDetailsHandle);
		if (result == Result.Success)
		{
			JoinLobby(outLobbyDetailsHandle);
		}
		else
		{
			Debug.LogFormat("failed getting lobby details from invite, result = " + result.ToString());
		}
	}

	private void OnShowFriendsComplete(ref ShowFriendsCallbackInfo data)
	{
		_ = data.GetResultCode() == Result.Success;
	}

	private void OnLobbyUpdated(ref UpdateLobbyCallbackInfo data)
	{
		if (data.GetResultCode() == Result.Success)
		{
			OnLobbyChanged();
		}
	}

	private void TryGetLobbyDetails()
	{
		if (currentLobbyDetails != null)
		{
			currentLobbyDetails.Release();
			currentLobbyDetails = null;
		}
		CopyLobbyDetailsHandleOptions copyLobbyDetailsHandleOptions = default(CopyLobbyDetailsHandleOptions);
		copyLobbyDetailsHandleOptions.LobbyId = currentLobbyId;
		copyLobbyDetailsHandleOptions.LocalUserId = EOSLoginManager.loggedInProductId;
		CopyLobbyDetailsHandleOptions options = copyLobbyDetailsHandleOptions;
		_ = lobbyInterface?.CopyLobbyDetailsHandle(ref options, out currentLobbyDetails) == Result.Success;
		TryGetLobbyModificationHandle();
	}

	private void TryGetLobbyModificationHandle()
	{
		if (currentLobbyModificationHandle != null)
		{
			currentLobbyModificationHandle.Release();
			currentLobbyModificationHandle = null;
		}
		UpdateLobbyModificationOptions updateLobbyModificationOptions = default(UpdateLobbyModificationOptions);
		updateLobbyModificationOptions.LobbyId = currentLobbyId;
		updateLobbyModificationOptions.LocalUserId = EOSLoginManager.loggedInProductId;
		UpdateLobbyModificationOptions options = updateLobbyModificationOptions;
		_ = lobbyInterface?.UpdateLobbyModification(ref options, out currentLobbyModificationHandle) == Result.Success;
	}

	public bool RequestLobbyList(object requester, Filter filter, Action<List<LobbyDetails>> callback)
	{
		if (requester != null)
		{
			foreach (LobbyRefreshRequest lobbyRefreshRequest2 in lobbyRefreshRequests)
			{
				if (requester == lobbyRefreshRequest2.requester)
				{
					return false;
				}
			}
		}
		LobbyRefreshRequest lobbyRefreshRequest = default(LobbyRefreshRequest);
		lobbyRefreshRequest.requester = requester;
		lobbyRefreshRequest.filter = filter;
		lobbyRefreshRequest.callback = callback;
		LobbyRefreshRequest item = lobbyRefreshRequest;
		lobbyRefreshRequests.Enqueue(item);
		UpdateRefreshRequestQueue();
		return true;
	}

	private void UpdateRefreshRequestQueue()
	{
		if (!currentRefreshRequest.HasValue && lobbyRefreshRequests.Count != 0)
		{
			SearchForLobbiesWithRequest(lobbyRefreshRequests.Dequeue());
		}
	}

	private void SearchForLobbiesWithRequest(LobbyRefreshRequest request)
	{
		if (currentSearchHandle != null)
		{
			currentSearchHandle.Release();
			currentSearchHandle = null;
		}
		CreateLobbySearchOptions createLobbySearchOptions = default(CreateLobbySearchOptions);
		createLobbySearchOptions.MaxResults = 50u;
		CreateLobbySearchOptions options = createLobbySearchOptions;
		currentSearchHandle = new LobbySearch();
		Result? result = lobbyInterface?.CreateLobbySearch(ref options, out currentSearchHandle);
		if (result == Result.Success)
		{
			LobbySearchSetParameterOptions lobbySearchSetParameterOptions = default(LobbySearchSetParameterOptions);
			lobbySearchSetParameterOptions.ComparisonOp = ComparisonOp.Equal;
			lobbySearchSetParameterOptions.Parameter = new AttributeData
			{
				Key = "build_id",
				Value = RoR2Application.GetBuildId()
			};
			LobbySearchSetParameterOptions options2 = lobbySearchSetParameterOptions;
			currentSearchHandle.SetParameter(ref options2);
			foreach (AttributeData searchDatum in request.filter.SearchData)
			{
				lobbySearchSetParameterOptions = default(LobbySearchSetParameterOptions);
				lobbySearchSetParameterOptions.ComparisonOp = ComparisonOp.Equal;
				lobbySearchSetParameterOptions.Parameter = searchDatum;
				options2 = lobbySearchSetParameterOptions;
				currentSearchHandle.SetParameter(ref options2);
			}
			LobbySearchFindOptions lobbySearchFindOptions = default(LobbySearchFindOptions);
			lobbySearchFindOptions.LocalUserId = EOSLoginManager.loggedInProductId;
			LobbySearchFindOptions options3 = lobbySearchFindOptions;
			currentSearchHandle.Find(ref options3, request.callback, OnLobbySearchComplete);
		}
		else
		{
			Debug.LogError("Error Creating Lobby Search Handle! " + result.ToString());
		}
	}

	private void OnStopClient()
	{
		NetworkConnection connection = NetworkManagerSystem.singleton.client.connection;
		bool flag = false;
		bool flag2 = Util.ConnectionIsLocal(connection);
		flag = ((!(connection is EOSNetworkConnection)) ? (connection.address == newestLobbyData.serverAddressPortPair.address) : (((EOSNetworkConnection)connection).RemoteUserID == newestLobbyData.serverId.productUserID));
		if (flag2 && ownsLobby && currentLobbyModificationHandle != null)
		{
			LobbyModificationRemoveAttributeOptions lobbyModificationRemoveAttributeOptions = default(LobbyModificationRemoveAttributeOptions);
			lobbyModificationRemoveAttributeOptions.Key = "server_id";
			LobbyModificationRemoveAttributeOptions options = lobbyModificationRemoveAttributeOptions;
			currentLobbyModificationHandle.RemoveAttribute(ref options);
		}
		if (!flag2 && flag)
		{
			LeaveLobby();
		}
	}

	private void OnStartHostingServer()
	{
		hostingServer = true;
	}

	private void OnStopHostingServer()
	{
		hostingServer = false;
	}

	public void JoinOrStartMigrate(PlatformID newLobbyId)
	{
		if (ownsLobby)
		{
			StartMigrateLobby(newLobbyId.productUserID);
		}
		else
		{
			JoinLobby(newLobbyId);
		}
	}

	public void StartMigrateLobby(ProductUserId newLobbyId)
	{
	}

	private void AttemptToJoinPendingSteamworksLobby()
	{
		_ = LocalUserManager.isAnyUserSignedIn;
	}

	public void SetLobbyQuickPlayQueuedIfOwner(bool quickplayQueuedState)
	{
	}

	public void SetLobbyQuickPlayCutoffTimeIfOwner(uint? timestamp)
	{
	}

	public void SetStartingIfOwner(bool startingState)
	{
	}

	protected void OnLobbyOwnershipGained()
	{
		onLobbyOwnershipGained?.Invoke();
	}

	private void OnLobbyOwnershipLost()
	{
		onLobbyOwnershipLost?.Invoke();
	}

	public override bool IsLobbyOwner(PlatformID user)
	{
		LobbyDetailsGetLobbyOwnerOptions options = default(LobbyDetailsGetLobbyOwnerOptions);
		if (currentLobbyDetails != null)
		{
			return currentLobbyDetails.GetLobbyOwner(ref options) == user.productUserID;
		}
		return false;
	}

	public override void AutoMatchmake()
	{
	}

	public override bool IsLobbyOwner()
	{
		if (isInLobby)
		{
			return _ownsLobby;
		}
		return false;
	}

	public override bool CanInvite()
	{
		return !IsBusy;
	}

	public override void OpenInviteOverlay()
	{
		UIInterface uIInterface = EOSPlatformManager.GetPlatformInterface().GetUIInterface();
		ShowFriendsOptions options = new ShowFriendsOptions
		{
			LocalUserId = EOSLoginManager.loggedInAuthId
		};
		uIInterface?.ShowFriends(ref options, null, OnShowFriendsComplete);
	}

	public override void OnStartPrivateGame()
	{
	}

	public override void ToggleQuickplay()
	{
	}

	public override void CheckIfInvited()
	{
	}

	public override void CheckBusyTimer()
	{
	}

	public override bool ShouldEnableQuickplayButton()
	{
		return false;
	}

	public override bool ShouldEnableStartPrivateGameButton()
	{
		return true;
	}

	public override string GetUserDisplayName(PlatformID user)
	{
		if (user.productUserID != null)
		{
			string userDisplayName = (PlatformSystems.userManager as UserManagerEOS).GetUserDisplayName(user);
			if (userDisplayName != string.Empty)
			{
				return userDisplayName;
			}
		}
		return "";
	}

	public string GetUserDisplayNameFromProductIdString(string productIdString, Action callback = null)
	{
		if (productIdString != string.Empty)
		{
			string userDisplayName = (PlatformSystems.userManager as UserManagerEOS).GetUserDisplayName(new PlatformID(productIdString), callback);
			if (userDisplayName != string.Empty)
			{
				return userDisplayName;
			}
		}
		return "";
	}

	private static LobbyType PermissionLevelToType(LobbyPermissionLevel permissionLevel)
	{
		return permissionLevel switch
		{
			LobbyPermissionLevel.Inviteonly => LobbyType.Private, 
			LobbyPermissionLevel.Publicadvertised => LobbyType.Public, 
			LobbyPermissionLevel.Joinviapresence => LobbyType.FriendsOnly, 
			_ => LobbyType.Error, 
		};
	}

	private static LobbyPermissionLevel LobbyTypeToPermissionLevel(LobbyType type)
	{
		return type switch
		{
			LobbyType.FriendsOnly => LobbyPermissionLevel.Joinviapresence, 
			LobbyType.Private => LobbyPermissionLevel.Joinviapresence, 
			LobbyType.Public => LobbyPermissionLevel.Publicadvertised, 
			_ => LobbyPermissionLevel.Joinviapresence, 
		};
	}

	public override string GetLobbyID()
	{
		return currentLobbyId;
	}

	public override bool CheckLobbyIdValidity(string lobbyID)
	{
		if (lobbyID.IndexOfAny(charactersToCheck) == -1)
		{
			return false;
		}
		return true;
	}

	public override void JoinLobby(ConCommandArgs args)
	{
		CheckIfInitializedAndValid();
		string text = args[0];
		Debug.LogFormat("Enqueuing join for lobby {0}...", text);
		FindClipboardLobby(text);
	}

	public override void LobbyCreate(ConCommandArgs args)
	{
		EOSLobbyManager obj = PlatformSystems.lobbyManager as EOSLobbyManager;
		obj.CheckIfInitializedAndValid();
		if (!LocalUserManager.isAnyUserSignedIn)
		{
			throw new ConCommandException("Cannot create a Steamworks lobby without any local users signed in.");
		}
		obj.CreateLobby();
	}

	public override void LobbyCreateIfNone(ConCommandArgs args)
	{
		EOSLobbyManager obj = PlatformSystems.lobbyManager as EOSLobbyManager;
		obj.CheckIfInitializedAndValid();
		if (!LocalUserManager.isAnyUserSignedIn)
		{
			throw new ConCommandException("Cannot create a Steamworks lobby without any local users signed in.");
		}
		obj.CreateLobby();
	}

	public override void LobbyLeave(ConCommandArgs args)
	{
		EOSLobbyManager obj = PlatformSystems.lobbyManager as EOSLobbyManager;
		obj.CheckIfInitializedAndValid();
		obj.LeaveLobby();
	}

	public override void LobbyAssignOwner(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as EOSLobbyManager).CheckIfInitializedAndValid();
		Debug.LogFormat("Promoting {0} to lobby leader...", args[0]);
	}

	public override void LobbyInvite(ConCommandArgs args)
	{
		EOSLobbyManager eOSLobbyManager = PlatformSystems.lobbyManager as EOSLobbyManager;
		eOSLobbyManager.CheckIfInitializedAndValid();
		SendInviteOptions sendInviteOptions = default(SendInviteOptions);
		sendInviteOptions.LobbyId = eOSLobbyManager.currentLobbyId;
		sendInviteOptions.LocalUserId = EOSLoginManager.loggedInProductId;
		sendInviteOptions.TargetUserId = ProductUserId.FromString(args.GetArgString(0));
		SendInviteOptions options = sendInviteOptions;
		eOSLobbyManager.lobbyInterface?.SendInvite(ref options, null, OnSendInviteComplete);
	}

	public override void LobbyOpenInviteOverlay(ConCommandArgs args)
	{
		if (EOSLoginManager.loggedInAuthId == null)
		{
			SteamworksLobbyManager.DoSteamLobbyOpenOverlay();
			return;
		}
		EOSLobbyManager obj = PlatformSystems.lobbyManager as EOSLobbyManager;
		obj.CheckIfInitializedAndValid();
		obj.OpenInviteOverlay();
	}

	public override void LobbyCopyToClipboard(ConCommandArgs args)
	{
		EOSLobbyManager obj = PlatformSystems.lobbyManager as EOSLobbyManager;
		obj.CheckIfInitializedAndValid();
		GUIUtility.systemCopyBuffer = obj.currentLobbyId;
		Chat.AddMessage(Language.GetString("STEAM_COPY_LOBBY_TO_CLIPBOARD_MESSAGE"));
	}

	public override void LobbyPrintData(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as EOSLobbyManager).CheckIfInitializedAndValid();
		new List<string>();
	}

	public override void DisplayId(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as EOSLobbyManager).CheckIfInitializedAndValid();
	}

	public override void DisplayLobbyId(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as EOSLobbyManager).CheckIfInitializedAndValid();
	}

	public override void LobbyPrintMembers(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as EOSLobbyManager).CheckIfInitializedAndValid();
	}

	public override void ClearLobbies(ConCommandArgs args)
	{
		EOSLobbyManager obj = PlatformSystems.lobbyManager as EOSLobbyManager;
		Filter filter = new Filter();
		obj.RequestLobbyList(null, filter, delegate(List<LobbyDetails> lobbies)
		{
			foreach (LobbyDetails lobby in lobbies)
			{
				_ = lobby;
			}
		});
	}

	public override void LobbyUpdatePlayerCount(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as EOSLobbyManager).UpdatePlayerCount();
	}

	public override void LobbyForceUpdateData(ConCommandArgs args)
	{
		EOSLobbyManager obj = PlatformSystems.lobbyManager as EOSLobbyManager;
		obj.CheckIfInitializedAndValid();
		obj.ForceLobbyDataUpdate();
	}

	public override void LobbyPrintList(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as EOSLobbyManager).CheckIfInitializedAndValid();
	}

	public static bool IsLobbyOwner(LobbyDetails lobby)
	{
		LobbyDetailsGetLobbyOwnerOptions options = default(LobbyDetailsGetLobbyOwnerOptions);
		return lobby.GetLobbyOwner(ref options) == EOSLoginManager.loggedInProductId;
	}

	public static string GetLobbyStringValue(LobbyDetails lobby, string key)
	{
		LobbyDetailsCopyAttributeByKeyOptions lobbyDetailsCopyAttributeByKeyOptions = default(LobbyDetailsCopyAttributeByKeyOptions);
		lobbyDetailsCopyAttributeByKeyOptions.AttrKey = key;
		LobbyDetailsCopyAttributeByKeyOptions options = lobbyDetailsCopyAttributeByKeyOptions;
		if (lobby.CopyAttributeByKey(ref options, out var outAttribute) == Result.Success)
		{
			return outAttribute.Value.Data.Value.Value.AsUtf8;
		}
		return string.Empty;
	}

	public static bool GetLobbyBoolValue(LobbyDetails lobbyDetails, string key)
	{
		LobbyDetailsCopyAttributeByKeyOptions lobbyDetailsCopyAttributeByKeyOptions = default(LobbyDetailsCopyAttributeByKeyOptions);
		lobbyDetailsCopyAttributeByKeyOptions.AttrKey = key;
		LobbyDetailsCopyAttributeByKeyOptions options = lobbyDetailsCopyAttributeByKeyOptions;
		if (lobbyDetails.CopyAttributeByKey(ref options, out var outAttribute) == Result.Success)
		{
			if (outAttribute.Value.Data.Value.Value.AsBool.HasValue)
			{
				return outAttribute.Value.Data.Value.Value.AsBool.Value;
			}
			return false;
		}
		return false;
	}

	public static long GetLobbyIntValue(LobbyDetails lobbyDetails, string key)
	{
		LobbyDetailsCopyAttributeByKeyOptions lobbyDetailsCopyAttributeByKeyOptions = default(LobbyDetailsCopyAttributeByKeyOptions);
		lobbyDetailsCopyAttributeByKeyOptions.AttrKey = key;
		LobbyDetailsCopyAttributeByKeyOptions options = lobbyDetailsCopyAttributeByKeyOptions;
		if (lobbyDetails.CopyAttributeByKey(ref options, out var outAttribute) == Result.Success && outAttribute.Value.Data.Value.Value.AsInt64.HasValue)
		{
			return outAttribute.Value.Data.Value.Value.AsInt64.Value;
		}
		return 0L;
	}

	public static double GetLobbyDoubleValue(LobbyDetails lobbyDetails, string key)
	{
		LobbyDetailsCopyAttributeByKeyOptions lobbyDetailsCopyAttributeByKeyOptions = default(LobbyDetailsCopyAttributeByKeyOptions);
		lobbyDetailsCopyAttributeByKeyOptions.AttrKey = key;
		LobbyDetailsCopyAttributeByKeyOptions options = lobbyDetailsCopyAttributeByKeyOptions;
		if (lobbyDetails.CopyAttributeByKey(ref options, out var outAttribute) == Result.Success && outAttribute.Value.Data.Value.Value.AsDouble.HasValue)
		{
			return outAttribute.Value.Data.Value.Value.AsDouble.Value;
		}
		return 0.0;
	}

	public static bool SetLobbyStringValue(LobbyModification lobby, string key, string value)
	{
		if (lobby == null || key == null || value == null || key == string.Empty || value == string.Empty)
		{
			return false;
		}
		LobbyModificationAddAttributeOptions lobbyModificationAddAttributeOptions = default(LobbyModificationAddAttributeOptions);
		lobbyModificationAddAttributeOptions.Attribute = new AttributeData
		{
			Key = key,
			Value = value
		};
		lobbyModificationAddAttributeOptions.Visibility = LobbyAttributeVisibility.Public;
		LobbyModificationAddAttributeOptions options = lobbyModificationAddAttributeOptions;
		return lobby.AddAttribute(ref options) == Result.Success;
	}

	public static void UpdateLobby(LobbyModification lobby)
	{
		EOSLobbyManager eOSLobbyManager = PlatformSystems.lobbyManager as EOSLobbyManager;
		eOSLobbyManager.CheckIfInitializedAndValid();
		UpdateLobbyOptions updateLobbyOptions = default(UpdateLobbyOptions);
		updateLobbyOptions.LobbyModificationHandle = lobby;
		UpdateLobbyOptions options = updateLobbyOptions;
		eOSLobbyManager.lobbyInterface?.UpdateLobby(ref options, null, eOSLobbyManager.OnLobbyUpdated);
	}

	public static bool RemoveLobbyStringValue(LobbyModification lobby, string key)
	{
		if (lobby != null)
		{
			LobbyModificationRemoveAttributeOptions lobbyModificationRemoveAttributeOptions = default(LobbyModificationRemoveAttributeOptions);
			lobbyModificationRemoveAttributeOptions.Key = key;
			LobbyModificationRemoveAttributeOptions options = lobbyModificationRemoveAttributeOptions;
			return lobby.RemoveAttribute(ref options) == Result.Success;
		}
		return false;
	}

	public static uint GetAllCurrentLobbyKVPs(LobbyDetails lobby, ref Dictionary<string, string> KVPs)
	{
		LobbyDetailsGetAttributeCountOptions options = default(LobbyDetailsGetAttributeCountOptions);
		uint attributeCount = lobby.GetAttributeCount(ref options);
		uint num = 0u;
		for (uint num2 = attributeCount; num < num2; num++)
		{
			LobbyDetailsCopyAttributeByIndexOptions lobbyDetailsCopyAttributeByIndexOptions = default(LobbyDetailsCopyAttributeByIndexOptions);
			lobbyDetailsCopyAttributeByIndexOptions.AttrIndex = num;
			LobbyDetailsCopyAttributeByIndexOptions options2 = lobbyDetailsCopyAttributeByIndexOptions;
			if (lobby.CopyAttributeByIndex(ref options2, out var outAttribute) == Result.Success)
			{
				AttributeData value = outAttribute.Value.Data.Value;
				KVPs.Add(value.Key, value.Value.AsUtf8);
			}
		}
		return attributeCount;
	}

	public override void SetLobbyTypeConVarString(string newValue)
	{
		EOSLobbyManager eOSLobbyManager = PlatformSystems.lobbyManager as EOSLobbyManager;
		eOSLobbyManager.CheckIfInitializedAndValid();
		LobbyDetailsCopyInfoOptions options = default(LobbyDetailsCopyInfoOptions);
		if (eOSLobbyManager.CurrentLobbyDetails.CopyInfo(ref options, out var _) != 0 || !eOSLobbyManager.IsLobbyOwner())
		{
			throw new ConCommandException("Lobby type cannot be set while not the owner of a valid lobby.");
		}
		LobbyType dest = LobbyType.Error;
		SteamLobbyTypeConVar.instance.GetEnumValueAbstract(newValue, ref dest);
		if (dest == LobbyType.Error)
		{
			throw new ConCommandException("Lobby type \"Error\" is not allowed.");
		}
		LobbyModificationSetPermissionLevelOptions lobbyModificationSetPermissionLevelOptions = default(LobbyModificationSetPermissionLevelOptions);
		lobbyModificationSetPermissionLevelOptions.PermissionLevel = LobbyTypeToPermissionLevel(dest);
		LobbyModificationSetPermissionLevelOptions options2 = lobbyModificationSetPermissionLevelOptions;
		if (eOSLobbyManager.CurrentLobbyModification.SetPermissionLevel(ref options2) == Result.Success)
		{
			UpdateLobby(eOSLobbyManager.CurrentLobbyModification);
		}
	}

	public override string GetLobbyTypeConVarString()
	{
		EOSLobbyManager eOSLobbyManager = PlatformSystems.lobbyManager as EOSLobbyManager;
		eOSLobbyManager.CheckIfInitializedAndValid();
		if (eOSLobbyManager.currentLobbyDetails != null)
		{
			LobbyDetailsCopyInfoOptions options = default(LobbyDetailsCopyInfoOptions);
			if (eOSLobbyManager.CurrentLobbyDetails.CopyInfo(ref options, out var outLobbyDetailsInfo) == Result.Success)
			{
				return PermissionLevelToType(outLobbyDetailsInfo.Value.PermissionLevel).ToString();
			}
			return string.Empty;
		}
		return string.Empty;
	}

	public override int GetLobbyMemberCount()
	{
		return 0;
	}
}

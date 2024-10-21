using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Facepunch.Steamworks;
using HG;
using RoR2.Networking;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class SteamworksLobbyManager : PCLobbyManager
{
	private struct LobbyRefreshRequest
	{
		public object requester;

		public LobbyList.Filter filter;

		public Action<List<LobbyList.Lobby>> callback;
	}

	public bool isInLobbyDelayed;

	private const MPFeatures PlatformFeatureFlags = MPFeatures.HostGame | MPFeatures.FindGame;

	private const MPLobbyFeatures PlatformLobbyUIFlags = MPLobbyFeatures.CreateLobby | MPLobbyFeatures.SocialIcon | MPLobbyFeatures.HostPromotion | MPLobbyFeatures.Clipboard | MPLobbyFeatures.Invite | MPLobbyFeatures.UserIcon | MPLobbyFeatures.LeaveLobby | MPLobbyFeatures.LobbyDropdownOptions;

	private PlatformID _pendingSteamworksLobbyId;

	private bool _ownsLobby;

	private int minimumPlayerCount = 2;

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

	private readonly List<int> playerCountsList = new List<int>();

	private MemoizedToString<int, ToStringImplementationInvariant> localPlayerCountToString = MemoizedToString<int, ToStringImplementationInvariant>.GetNew();

	private bool startingFadeSet;

	private readonly MethodInfo updateLobbyDataMethodInfo = typeof(Lobby).GetMethod("UpdateLobbyData", BindingFlags.Instance | BindingFlags.NonPublic);

	private PlatformID lastHostingLobbyId;

	private Queue<LobbyRefreshRequest> lobbyRefreshRequests = new Queue<LobbyRefreshRequest>();

	private LobbyRefreshRequest? currentRefreshRequest;

	private bool hostingServer;

	private PlatformID currentServerId;

	private MemoizedToString<PlatformID, ToStringDefault<PlatformID>> currentServerIdString = MemoizedToString<PlatformID, ToStringDefault<PlatformID>>.GetNew();

	public PlatformID pendingSteamworksLobbyId
	{
		get
		{
			return _pendingSteamworksLobbyId;
		}
		set
		{
			if (!_pendingSteamworksLobbyId.Equals(value))
			{
				if (_pendingSteamworksLobbyId.value != null)
				{
					RoR2Application.onUpdate -= AttemptToJoinPendingSteamworksLobby;
				}
				_pendingSteamworksLobbyId = value;
				if (_pendingSteamworksLobbyId.value != null)
				{
					RoR2Application.onUpdate += AttemptToJoinPendingSteamworksLobby;
				}
			}
		}
	}

	private Client client => Client.Instance;

	public override bool isInLobby
	{
		get
		{
			if (client == null || client.Lobby == null)
			{
				return false;
			}
			return client.Lobby.IsValid;
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

	public PlatformID serverId => newestLobbyData.serverId;

	public override LobbyData newestLobbyData { get; protected set; }

	public override int calculatedTotalPlayerCount { get; protected set; }

	public override int calculatedExtraPlayersCount { get; protected set; }

	public override LobbyType currentLobbyType
	{
		get
		{
			return (LobbyType)Client.Instance.Lobby.LobbyType;
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

	public static SteamworksLobbyManager GetFromPlatformSystems()
	{
		return PlatformSystems.lobbyManager as SteamworksLobbyManager;
	}

	private void UpdateOwnsLobby()
	{
		ownsLobby = client.Lobby.IsOwner;
	}

	public void Init()
	{
		Client instance = Client.Instance;
		instance.Lobby.OnChatMessageRecieved = OnChatMessageReceived;
		instance.Lobby.OnLobbyCreated = OnLobbyCreated;
		instance.Lobby.OnLobbyDataUpdated = OnLobbyDataUpdated;
		instance.Lobby.OnLobbyJoined = OnLobbyJoined;
		instance.Lobby.OnLobbyMemberDataUpdated = OnLobbyMemberDataUpdated;
		instance.Lobby.OnLobbyStateChanged = OnLobbyStateChanged;
		instance.Lobby.OnLobbyKicked = OnLobbyKicked;
		instance.Lobby.OnLobbyLeave = OnLobbyLeave;
		instance.Lobby.OnUserInvitedToLobby = OnUserInvitedToLobby;
		instance.Lobby.OnLobbyJoinRequested = OnLobbyJoinRequested;
		instance.LobbyList.OnLobbiesUpdated = OnLobbiesUpdated;
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
		SetStartingIfOwner(startingState: false);
		pendingSteamworksLobbyId = GetLaunchParamsLobbyId();
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
		if (Client.Instance == null)
		{
			throw new ConCommandException("Steamworks client not available.");
		}
	}

	private void UpdatePlayerCount()
	{
		if (client != null && client.Lobby.IsValid)
		{
			int count = LocalUserManager.readOnlyLocalUsersList.Count;
			string memberData = client.Lobby.GetMemberData(client.SteamId, "player_count");
			string @string = localPlayerCountToString.GetString(Math.Max(1, count));
			if (memberData != @string)
			{
				client.Lobby.SetMemberData("player_count", @string);
			}
			playerCountsList.Clear();
			calculatedTotalPlayerCount = 0;
			remoteMachineCount = 0;
			calculatedExtraPlayersCount = 0;
			ulong steamId = client.SteamId;
			ulong[] memberIDs = client.Lobby.GetMemberIDs();
			foreach (ulong num in memberIDs)
			{
				int result = ((!TextSerialization.TryParseInvariant(client.Lobby.GetMemberData(num, "player_count"), out result)) ? 1 : Math.Max(1, result));
				if (num == steamId)
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
		isInLobby = client.Lobby.IsValid;
		isInLobbyDelayed = isInLobby;
		UpdateOwnsLobby();
		UpdatePlayerCount();
		onLobbyChanged?.Invoke();
		OnLobbyDataUpdated();
	}

	public override void CreateLobby()
	{
		if (client != null)
		{
			pendingSteamworksLobbyId = default(PlatformID);
			client.Lobby.Leave();
			base.awaitingCreate = true;
			client.Lobby.Create((Lobby.Type)preferredLobbyType, RoR2Application.maxPlayers);
		}
	}

	public override void JoinLobby(PlatformID uid)
	{
		PlatformID platformID = uid;
		if (client != null && LocalUserManager.isAnyUserSignedIn)
		{
			base.awaitingJoin = true;
			LeaveLobby();
			client.Lobby.Join(platformID.ID);
		}
	}

	public override void LeaveLobby()
	{
		client?.Lobby.Leave();
	}

	public override PlatformID[] GetLobbyMembers()
	{
		ulong[] array = Client.Instance?.Lobby?.GetMemberIDs();
		PlatformID[] array2 = null;
		if (array != null)
		{
			array2 = new PlatformID[array.Length];
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = new PlatformID(array[i]);
			}
		}
		return array2;
	}

	public override bool ShouldShowPromoteButton()
	{
		return SteamLobbyFinder.running;
	}

	private void Update()
	{
		if (client != null && startingFadeSet != (newestLobbyData.starting && !ClientScene.ready))
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
		if (client != null)
		{
			UpdateOwnsLobby();
		}
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
		Lobby lobby = client?.Lobby;
		if (lobby != null)
		{
			updateLobbyDataMethodInfo.Invoke(lobby, Array.Empty<object>());
		}
	}

	public void SendLobbyMessage(LobbyMessageType messageType, NetworkWriter writer)
	{
		byte[] array = new byte[1 + writer.Position];
		array[0] = (byte)messageType;
		Array.Copy(writer.AsArray(), 0, array, 1, writer.Position);
		Client.Instance.Lobby.SendChatMessage(Encoding.UTF8.GetString(array));
	}

	public override void OnCutoffTimerComplete()
	{
	}

	private void OnChatMessageReceived(ulong senderId, byte[] buffer, int byteCount)
	{
		try
		{
			_ = Client.Instance.Lobby.IsOwner;
			bool flag = senderId == Client.Instance.Lobby.Owner;
			bool flag2 = senderId == Client.Instance.SteamId;
			NetworkReader networkReader = new NetworkReader(buffer);
			if (byteCount >= 1)
			{
				LobbyMessageType lobbyMessageType = (LobbyMessageType)networkReader.ReadByte();
				Debug.LogFormat("Received Steamworks Lobby Message from {0} ({1}B). messageType={2}", senderId, byteCount, lobbyMessageType);
				switch (lobbyMessageType)
				{
				case LobbyMessageType.Chat:
				{
					string arg = networkReader.ReadString();
					Chat.AddMessage(string.Format("{0}: {1}", Client.Instance.Friends.Get(senderId)?.Name ?? "???", arg));
					break;
				}
				case LobbyMessageType.Password:
				{
					string @string = networkReader.ReadString();
					if (!flag2 && flag)
					{
						NetworkManagerSystem.cvClPassword.SetString(@string);
					}
					break;
				}
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

	private void OnLobbyCreated(bool success)
	{
		base.awaitingCreate = false;
		if (success)
		{
			Debug.LogFormat("Steamworks lobby creation succeeded. Lobby id = {0}", client.Lobby.CurrentLobby);
			OnLobbyChanged();
		}
	}

	protected void OnLobbyDataUpdated()
	{
		Lobby lobby = client.Lobby;
		newestLobbyData = (lobby.IsValid ? new LobbyData(lobby) : new LobbyData());
		UpdateOwnsLobby();
		UpdatePlayerCount();
		PlatformID platformID = new PlatformID(lobby.CurrentLobby);
		if (lobby.IsValid && !ownsLobby)
		{
			if (newestLobbyData.serverId.isValid)
			{
				bool num = newestLobbyData.serverId == NetworkManagerSystem.singleton.serverP2PId || NetworkManagerSystem.singleton.IsConnectedToServer(newestLobbyData.serverId);
				bool flag = string.CompareOrdinal(RoR2Application.GetBuildId(), newestLobbyData.buildId) == 0;
				if (!num && flag)
				{
					NetworkManagerSystem.singleton.desiredHost = new HostDescription(newestLobbyData.serverId, HostDescription.HostType.Steam);
					lastHostingLobbyId = platformID;
				}
			}
			else if (lastHostingLobbyId == platformID)
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

	private void OnLobbyJoined(bool success)
	{
		base.awaitingJoin = false;
		if (success)
		{
			if (client.Lobby.CurrentLobbyData != null)
			{
				string buildId = RoR2Application.GetBuildId();
				string data = client.Lobby.CurrentLobbyData.GetData("build_id");
				if (buildId != data)
				{
					Debug.LogFormat("Lobby build_id mismatch, leaving lobby. Ours=\"{0}\" Theirs=\"{1}\"", buildId, data);
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
					object[] formatParams = new string[2] { buildId, data };
					descriptionToken.formatParams = formatParams;
					simpleDialogBox.descriptionToken = descriptionToken;
					client.Lobby.Leave();
					return;
				}
			}
			Debug.LogFormat("Steamworks lobby join succeeded. Lobby id = {0}", client.Lobby.CurrentLobby);
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
		onLobbyJoined?.Invoke(success);
	}

	private void OnLobbyMemberDataUpdated(ulong memberId)
	{
		UpdateOwnsLobby();
		onLobbyMemberDataUpdated?.Invoke(new PlatformID(memberId));
	}

	protected void OnLobbyStateChanged(Lobby.MemberStateChange memberStateChange, ulong initiatorUserId, ulong affectedUserId)
	{
		Debug.LogFormat("OnLobbyStateChanged memberStateChange={0} initiatorUserId={1} affectedUserId={2}", memberStateChange, initiatorUserId, affectedUserId);
		OnLobbyChanged();
		onLobbyStateChanged?.Invoke();
	}

	private void OnLobbyKicked(bool kickedDueToDisconnect, ulong lobbyId, ulong adminId)
	{
		Debug.LogFormat("Kicked from lobby. kickedDueToDisconnect={0} lobbyId={1} adminId={2}", kickedDueToDisconnect, lobbyId, adminId);
		OnLobbyChanged();
	}

	private void OnLobbyLeave(ulong lobbyId)
	{
		Debug.LogFormat("Left lobby {0}.", lobbyId);
		onLobbyLeave?.Invoke(new PlatformID(lobbyId));
		OnLobbyChanged();
	}

	private void OnLobbyJoinRequested(ulong lobbyId)
	{
		Debug.LogFormat("Request to join lobby {0} received. Attempting to join lobby.", lobbyId);
		pendingSteamworksLobbyId = new PlatformID(lobbyId);
	}

	private void OnUserInvitedToLobby(ulong lobbyId, ulong senderId)
	{
		Debug.LogFormat("Received invitation to lobby {0} from sender {1}.", lobbyId, senderId);
	}

	public bool RequestLobbyList(object requester, LobbyList.Filter filter, Action<List<LobbyList.Lobby>> callback)
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
			currentRefreshRequest = lobbyRefreshRequests.Dequeue();
			client.LobbyList.Refresh(currentRefreshRequest.Value.filter);
		}
	}

	private void OnLobbiesUpdated()
	{
		if (currentRefreshRequest.HasValue)
		{
			LobbyRefreshRequest value = currentRefreshRequest.Value;
			currentRefreshRequest = null;
			List<LobbyList.Lobby> lobbies = client.LobbyList.Lobbies;
			value.callback(lobbies);
		}
		onLobbiesUpdated?.Invoke();
		UpdateRefreshRequestQueue();
	}

	private void OnStopClient()
	{
		if ((bool)NetworkManagerSystem.singleton && NetworkManagerSystem.singleton.client != null && NetworkManagerSystem.singleton.client.connection != null)
		{
			NetworkConnection connection = NetworkManagerSystem.singleton.client.connection;
			bool flag = false;
			bool flag2 = Util.ConnectionIsLocal(connection);
			flag = ((!(connection is SteamNetworkConnection)) ? (connection.address == newestLobbyData.serverAddressPortPair.address) : (((SteamNetworkConnection)connection).steamId == newestLobbyData.serverId));
			if (flag2 && ownsLobby)
			{
				client.Lobby.CurrentLobbyData.RemoveData("server_id");
			}
			if (!flag2 && flag)
			{
				client.Lobby.Leave();
			}
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
			StartMigrateLobby(newLobbyId);
			return;
		}
		client.Lobby.Leave();
		JoinLobby(newLobbyId);
	}

	public void StartMigrateLobby(PlatformID newLobbyId)
	{
		client.Lobby.Joinable = false;
		client.Lobby.CurrentLobbyData.SetData("migration_id", TextSerialization.ToStringInvariant(newLobbyId.ID));
	}

	private void AttemptToJoinPendingSteamworksLobby()
	{
		if (LocalUserManager.isAnyUserSignedIn && pendingSteamworksLobbyId != default(PlatformID))
		{
			JoinLobby(pendingSteamworksLobbyId);
			pendingSteamworksLobbyId = default(PlatformID);
		}
	}

	public void SetLobbyQuickPlayQueuedIfOwner(bool quickplayQueuedState)
	{
		Lobby lobby = client.Lobby;
		if (lobby?.CurrentLobbyData != null)
		{
			lobby.CurrentLobbyData.SetData("qp", quickplayQueuedState ? "1" : "0");
			if (!quickplayQueuedState)
			{
				lobby.LobbyType = (Lobby.Type)preferredLobbyType;
			}
		}
	}

	public void SetLobbyQuickPlayCutoffTimeIfOwner(uint? timestamp)
	{
		Lobby lobby = client.Lobby;
		if (lobby?.CurrentLobbyData != null)
		{
			if (!timestamp.HasValue)
			{
				lobby.CurrentLobbyData.RemoveData("qp_cutoff_time");
				return;
			}
			string v = TextSerialization.ToStringInvariant(timestamp.Value);
			lobby.CurrentLobbyData.SetData("qp_cutoff_time", v);
		}
	}

	public void SetStartingIfOwner(bool startingState)
	{
		Lobby lobby = client.Lobby;
		if (lobby?.CurrentLobbyData != null)
		{
			lobby.CurrentLobbyData?.SetData("starting", startingState ? "1" : "0");
		}
	}

	protected void OnLobbyOwnershipGained()
	{
		onLobbyOwnershipGained?.Invoke();
	}

	private void OnLobbyOwnershipLost()
	{
		onLobbyOwnershipLost?.Invoke();
	}

	public static void CreateCannotJoinSteamLobbyPopup()
	{
		SimpleDialogBox simpleDialogBox = SimpleDialogBox.Create();
		simpleDialogBox.headerToken = new SimpleDialogBox.TokenParamsPair
		{
			token = "EOS_CANNOT_JOIN_STEAM_LOBBY_HEADER",
			formatParams = Array.Empty<object>()
		};
		simpleDialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair
		{
			token = "EOS_CANNOT_JOIN_STEAM_LOBBY_MESSAGE",
			formatParams = Array.Empty<object>()
		};
		simpleDialogBox.AddCancelButton(CommonLanguageTokens.ok);
	}

	public override string GetLobbyID()
	{
		CheckIfInitializedAndValid();
		return TextSerialization.ToStringInvariant(client.Lobby.CurrentLobby);
	}

	public override bool CheckLobbyIdValidity(string lobbyID)
	{
		if (!IsDigitsOnly(lobbyID))
		{
			return false;
		}
		return true;
		static bool IsDigitsOnly(string str)
		{
			foreach (char c in str)
			{
				if (c < '0' || c > '9')
				{
					return false;
				}
			}
			return true;
		}
	}

	public override void JoinLobby(ConCommandArgs args)
	{
		string text = args[0];
		if (PlatformID.TryParse(text, out var result))
		{
			CheckIfInitializedAndValid();
			Debug.LogFormat("Enqueuing join for lobby {0}...", text);
			pendingSteamworksLobbyId = new PlatformID(result.ID);
		}
		else
		{
			Debug.LogFormat("Failed parsing lobby ID from {0}...", text);
		}
	}

	public override void LobbyCreate(ConCommandArgs args)
	{
		SteamworksLobbyManager obj = PlatformSystems.lobbyManager as SteamworksLobbyManager;
		obj.CheckIfInitializedAndValid();
		if (!LocalUserManager.isAnyUserSignedIn)
		{
			throw new ConCommandException("Cannot create a Steamworks lobby without any local users signed in.");
		}
		obj.CreateLobby();
	}

	public override void LobbyCreateIfNone(ConCommandArgs args)
	{
		SteamworksLobbyManager steamworksLobbyManager = PlatformSystems.lobbyManager as SteamworksLobbyManager;
		Client client = steamworksLobbyManager.client;
		steamworksLobbyManager.CheckIfInitializedAndValid();
		if (!LocalUserManager.isAnyUserSignedIn)
		{
			throw new ConCommandException("Cannot create a Steamworks lobby without any local users signed in.");
		}
		if (!client.Lobby.IsValid)
		{
			steamworksLobbyManager.CreateLobby();
		}
	}

	public override void LobbyLeave(ConCommandArgs args)
	{
		SteamworksLobbyManager obj = PlatformSystems.lobbyManager as SteamworksLobbyManager;
		obj.CheckIfInitializedAndValid();
		obj.pendingSteamworksLobbyId = default(PlatformID);
		obj.LeaveLobby();
	}

	public override void LobbyAssignOwner(ConCommandArgs args)
	{
		SteamworksLobbyManager obj = PlatformSystems.lobbyManager as SteamworksLobbyManager;
		Client client = obj.client;
		obj.CheckIfInitializedAndValid();
		Debug.LogFormat("Promoting {0} to lobby leader...", args[0]);
		client.Lobby.Owner = args.GetArgPlatformID(0).ID;
	}

	public override void LobbyInvite(ConCommandArgs args)
	{
		SteamworksLobbyManager obj = PlatformSystems.lobbyManager as SteamworksLobbyManager;
		Client client = obj.client;
		obj.CheckIfInitializedAndValid();
		client.Lobby.InviteUserToLobby(args.GetArgPlatformID(0).ID);
	}

	public static void DoSteamLobbyOpenOverlay()
	{
		Client instance = Client.Instance;
		NetworkManagerSystemSteam.CheckSteamworks();
		instance.Overlay.OpenInviteDialog(instance.Lobby.CurrentLobby);
	}

	public override void LobbyOpenInviteOverlay(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as SteamworksLobbyManager).CheckIfInitializedAndValid();
		DoSteamLobbyOpenOverlay();
	}

	public override void LobbyCopyToClipboard(ConCommandArgs args)
	{
		string lobbyID = GetLobbyID();
		if (!string.IsNullOrWhiteSpace(lobbyID))
		{
			GUIUtility.systemCopyBuffer = lobbyID;
		}
		Chat.AddMessage(Language.GetString("STEAM_COPY_LOBBY_TO_CLIPBOARD_MESSAGE"));
	}

	public override void LobbyPrintData(ConCommandArgs args)
	{
		SteamworksLobbyManager obj = PlatformSystems.lobbyManager as SteamworksLobbyManager;
		Client client = obj.client;
		obj.CheckIfInitializedAndValid();
		if (!client.Lobby.IsValid)
		{
			return;
		}
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, string> allDatum in client.Lobby.CurrentLobbyData.GetAllData())
		{
			list.Add($"\"{allDatum.Key}\" = \"{allDatum.Value}\"");
		}
	}

	public override void DisplayId(ConCommandArgs args)
	{
		SteamworksLobbyManager obj = PlatformSystems.lobbyManager as SteamworksLobbyManager;
		Client client = obj.client;
		obj.CheckIfInitializedAndValid();
		Debug.LogFormat("Steam id = {0}", client.SteamId);
	}

	public override void DisplayLobbyId(ConCommandArgs args)
	{
		SteamworksLobbyManager obj = PlatformSystems.lobbyManager as SteamworksLobbyManager;
		Client client = obj.client;
		obj.CheckIfInitializedAndValid();
		Debug.LogFormat("Lobby id = {0}", client.Lobby.CurrentLobby);
	}

	public override void LobbyPrintMembers(ConCommandArgs args)
	{
		SteamworksLobbyManager obj = PlatformSystems.lobbyManager as SteamworksLobbyManager;
		Client client = obj.client;
		obj.CheckIfInitializedAndValid();
		ulong[] memberIDs = client.Lobby.GetMemberIDs();
		string[] array = new string[memberIDs.Length];
		for (int i = 0; i < memberIDs.Length; i++)
		{
			array[i] = string.Format("[{0}]{1} id={2} name={3}", i, (client.Lobby.Owner == memberIDs[i]) ? "*" : " ", memberIDs[i], client.Friends.GetName(memberIDs[i]));
		}
	}

	public override void ClearLobbies(ConCommandArgs args)
	{
		SteamworksLobbyManager obj = PlatformSystems.lobbyManager as SteamworksLobbyManager;
		_ = obj.client;
		obj.RequestLobbyList(null, new LobbyList.Filter
		{
			MaxResults = 50,
			DistanceFilter = LobbyList.Filter.Distance.Worldwide
		}, delegate(List<LobbyList.Lobby> lobbies)
		{
			foreach (LobbyList.Lobby lobby in lobbies)
			{
				_ = lobby;
			}
		});
	}

	public override void LobbyUpdatePlayerCount(ConCommandArgs args)
	{
		(PlatformSystems.lobbyManager as SteamworksLobbyManager).UpdatePlayerCount();
	}

	public override void LobbyForceUpdateData(ConCommandArgs args)
	{
	}

	public override void LobbyPrintList(ConCommandArgs args)
	{
		SteamworksLobbyManager obj = PlatformSystems.lobbyManager as SteamworksLobbyManager;
		Client client = obj.client;
		obj.CheckIfInitializedAndValid();
		List<LobbyList.Lobby> lobbies = client.LobbyList.Lobbies;
		string[] array = new string[lobbies.Count];
		for (int i = 0; i < lobbies.Count; i++)
		{
			array[i] = $"[{i}] id={lobbies[i].LobbyID}\n      players={lobbies[i].NumMembers}/{lobbies[i].MemberLimit},\n      owner=\"{client.Friends.GetName(lobbies[i].Owner)}\"";
		}
	}

	public override bool IsLobbyOwner(PlatformID user)
	{
		if (Client.Instance.Lobby.Owner == user.ID)
		{
			return user != default(PlatformID);
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

	public override void OnStartPrivateGame()
	{
		CreateLobby();
	}

	public override void ToggleQuickplay()
	{
		Console.instance.SubmitCmd(null, SteamLobbyFinder.running ? "steam_quickplay_stop" : "steam_quickplay_start");
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
		if (!newestLobbyData.quickplayQueued)
		{
			return ownsLobby;
		}
		return false;
	}

	public override string GetUserDisplayName(PlatformID user)
	{
		Client instance = Client.Instance;
		string result = "none";
		if (instance != null)
		{
			result = instance.Friends.GetName(user.ID);
		}
		return result;
	}

	public override void OpenInviteOverlay()
	{
		SteamworksLobbyManager obj = PlatformSystems.lobbyManager as SteamworksLobbyManager;
		Client client = obj.client;
		obj.CheckIfInitializedAndValid();
		NetworkManagerSystemSteam.CheckSteamworks();
		client.Overlay.OpenInviteDialog(client.Lobby.CurrentLobby);
	}

	public override void SetLobbyTypeConVarString(string newValue)
	{
		(PlatformSystems.lobbyManager as SteamworksLobbyManager).CheckIfInitializedAndValid();
		if (Client.Instance.Lobby.LobbyType == Lobby.Type.Error || !Client.Instance.Lobby.IsOwner)
		{
			throw new ConCommandException("Lobby type cannot be set while not the owner of a valid lobby.");
		}
		Lobby.Type dest = Lobby.Type.Error;
		SteamLobbyTypeConVar.instance.GetEnumValueAbstract(newValue, ref dest);
		if (dest == Lobby.Type.Error)
		{
			throw new ConCommandException("Lobby type \"Error\" is not allowed.");
		}
		Client.Instance.Lobby.LobbyType = dest;
	}

	public override string GetLobbyTypeConVarString()
	{
		return Client.Instance?.Lobby.LobbyType.ToString() ?? string.Empty;
	}

	public override int GetLobbyMemberCount()
	{
		return (Client.Instance?.Lobby)?.NumMembers ?? 0;
	}
}

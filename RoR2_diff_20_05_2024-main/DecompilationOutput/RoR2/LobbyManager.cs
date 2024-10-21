using System;
using System.Collections.Generic;
using System.Globalization;
using Facepunch.Steamworks;
using RoR2.ConVar;
using RoR2.Networking;

namespace RoR2;

public abstract class LobbyManager
{
	public enum State
	{
		Idle,
		Hosting,
		Waiting
	}

	public enum LobbyMessageType : byte
	{
		Chat,
		Password
	}

	public class LobbyDataSetupState
	{
		public int totalMaxPlayers;

		public int totalPlayerCount;

		public bool quickplayQueued;

		public PlatformID lobbyId;

		public PlatformID serverId;

		public AddressPortPair serverAddressPortPair;

		public bool starting;

		public string buildId = "0";

		public DateTime? quickplayCutoffTime;

		public bool shouldConnect;

		public bool joinable;
	}

	public class LobbyData
	{
		public readonly int totalMaxPlayers;

		public readonly int totalPlayerCount;

		public readonly bool quickplayQueued;

		public readonly PlatformID lobbyId = PlatformID.nil;

		public readonly PlatformID serverId = PlatformID.nil;

		public readonly AddressPortPair serverAddressPortPair;

		public readonly bool starting;

		public readonly string buildId = "0";

		public readonly DateTime? quickplayCutoffTime;

		public readonly bool shouldConnect;

		public readonly bool joinable;

		public LobbyData(LobbyDataSetupState setupState)
		{
			totalMaxPlayers = setupState.totalMaxPlayers;
			totalPlayerCount = setupState.totalPlayerCount;
			quickplayQueued = setupState.quickplayQueued;
			lobbyId = setupState.lobbyId;
			serverId = setupState.serverId;
			serverAddressPortPair = setupState.serverAddressPortPair;
			starting = setupState.starting;
			buildId = setupState.buildId;
			quickplayCutoffTime = setupState.quickplayCutoffTime;
			shouldConnect = setupState.shouldConnect;
			joinable = setupState.joinable;
		}

		public LobbyData()
		{
		}

		public static bool TryParseUserID(string str, out PlatformID result)
		{
			if (PlatformID.TryParse(str, out var result2))
			{
				result = result2;
				return true;
			}
			result = PlatformID.nil;
			return false;
		}

		public LobbyData(Lobby lobby)
		{
			PlatformID platformID = new PlatformID(lobby.CurrentLobby);
			Lobby.LobbyData currentLobbyData = lobby.CurrentLobbyData;
			Dictionary<string, string> lobbyDataDictionary = currentLobbyData.GetAllData();
			lobbyId = platformID;
			ReadCSteamID("server_id", ref serverId);
			ReadAddressPortPair("server_address", ref serverAddressPortPair);
			ReadInt("total_max_players", ref totalMaxPlayers);
			ReadInt("player_count", ref totalPlayerCount);
			ReadBool("qp", ref quickplayQueued);
			ReadBool("starting", ref starting);
			ReadString("build_id", ref buildId);
			ReadNullableDate("qp_cutoff_time", out quickplayCutoffTime);
			joinable = true;
			joinable &= totalPlayerCount < totalMaxPlayers;
			joinable &= lobby.LobbyType == Lobby.Type.Public;
			shouldConnect = serverId.isValid || serverAddressPortPair.isValid;
			bool ReadAddressPortPair(string metaDataName, ref AddressPortPair field)
			{
				if (lobbyDataDictionary.TryGetValue(metaDataName, out var value2) && AddressPortPair.TryParse(value2, out var addressPortPair))
				{
					field = addressPortPair;
					return true;
				}
				return false;
			}
			bool ReadBool(string metaDataName, ref bool field)
			{
				if (lobbyDataDictionary.TryGetValue(metaDataName, out var value4) && TextSerialization.TryParseInvariant(value4, out int result3))
				{
					field = result3 != 0;
					return true;
				}
				return false;
			}
			bool ReadCSteamID(string metaDataName, ref PlatformID field)
			{
				if (lobbyDataDictionary.TryGetValue(metaDataName, out var value) && TryParseUserID(value, out var result))
				{
					field = result;
					return true;
				}
				return false;
			}
			bool ReadInt(string metaDataName, ref int field)
			{
				if (lobbyDataDictionary.TryGetValue(metaDataName, out var value3) && TextSerialization.TryParseInvariant(value3, out int result2))
				{
					field = result2;
					return true;
				}
				return false;
			}
			bool ReadNullableDate(string metaDataName, out DateTime? field)
			{
				if (lobbyDataDictionary.TryGetValue(metaDataName, out var value6) && TextSerialization.TryParseInvariant(value6, out uint result4))
				{
					field = Util.dateZero + TimeSpan.FromSeconds(result4);
					return true;
				}
				field = null;
				return false;
			}
			bool ReadString(string metaDataName, ref string field)
			{
				if (lobbyDataDictionary.TryGetValue(metaDataName, out var value5))
				{
					field = value5;
					return true;
				}
				return false;
			}
		}
	}

	public State state;

	public LobbyType preferredLobbyType;

	public Action onPlayerCountUpdated;

	public Action<bool> onLobbyJoined;

	public Action onLobbiesUpdated;

	public Action onLobbyOwnershipGained;

	public Action onLobbyOwnershipLost;

	public Action onLobbyChanged;

	public Action onLobbyDataUpdated;

	public Action<PlatformID> onLobbyLeave;

	public Action<PlatformID> onLobbyMemberDataUpdated;

	public Action onLobbyStateChanged;

	private bool shouldShowGenericSpinner;

	public static readonly IntConVar cvSteamLobbyMaxMembers = new IntConVar("steam_lobby_max_members", ConVarFlags.None, RoR2Application.maxPlayers.ToString(CultureInfo.InvariantCulture), "Sets the maximum number of players allowed in steam lobbies created by this machine.");

	public const float QuickplayWaitTimeInSeconds = 20f;

	public abstract int calculatedTotalPlayerCount { get; protected set; }

	public abstract int calculatedExtraPlayersCount { get; protected set; }

	public abstract bool isInLobby { get; protected set; }

	public abstract bool ownsLobby { get; protected set; }

	public abstract bool IsBusy { get; set; }

	public bool awaitingJoin { get; protected set; }

	public bool awaitingCreate { get; protected set; }

	public bool isFull { get; protected set; }

	public abstract bool hasMinimumPlayerCount { get; }

	public abstract LobbyType currentLobbyType { get; set; }

	public abstract LobbyData newestLobbyData { get; protected set; }

	public abstract MPFeatures GetPlatformMPFeatureFlags();

	public abstract MPLobbyFeatures GetPlatformMPLobbyFeatureFlags();

	public bool HasMPFeatures()
	{
		return GetPlatformMPFeatureFlags() != MPFeatures.None;
	}

	public bool HasMPLobbyUI()
	{
		return GetPlatformMPLobbyFeatureFlags() != MPLobbyFeatures.None;
	}

	public bool HasMPFeature(MPFeatures flags)
	{
		return GetPlatformMPFeatureFlags().HasFlag(flags);
	}

	public bool HasMPLobbyFeature(MPLobbyFeatures flags)
	{
		return GetPlatformMPLobbyFeatureFlags().HasFlag(flags);
	}

	public abstract int GetLobbyMemberPlayerCountByIndex(int memberIndex);

	public abstract void CreateLobby();

	public abstract string GetLobbyID();

	public virtual void SetNetworkType(bool isInternet)
	{
	}

	public virtual bool IsNetworkTypeInternet()
	{
		return true;
	}

	public virtual void Shutdown()
	{
	}

	public abstract void LeaveLobby();

	public abstract void JoinLobby(PlatformID join);

	public abstract void OnStartPrivateGame();

	public virtual void OnMultiplayerMenuEnabled(Action<PlatformID> onLobbyLeave)
	{
		if (!PlatformSystems.lobbyManager.isInLobby)
		{
			PlatformSystems.lobbyManager.CreateLobby();
		}
		LobbyManager lobbyManager = PlatformSystems.lobbyManager;
		lobbyManager.onLobbyLeave = (Action<PlatformID>)Delegate.Combine(lobbyManager.onLobbyLeave, onLobbyLeave);
	}

	public abstract void AutoMatchmake();

	public abstract void ToggleQuickplay();

	public abstract void OnCutoffTimerComplete();

	public abstract bool ShouldEnableQuickplayButton();

	public virtual bool IsQuickPlaySearching()
	{
		return false;
	}

	public abstract bool IsLobbyOwner(PlatformID user);

	public abstract string GetUserDisplayName(PlatformID user);

	public abstract bool IsLobbyOwner();

	public abstract PlatformID[] GetLobbyMembers();

	public abstract int GetLobbyMemberCount();

	public abstract bool ShouldShowPromoteButton();

	public abstract void CheckIfInitializedAndValid();

	public abstract void CheckIfInvited();

	public abstract bool CanInvite();

	public abstract void CheckBusyTimer();

	public abstract bool ShouldEnableStartPrivateGameButton();

	public virtual void OpenInviteOverlay()
	{
		throw new NotImplementedException();
	}

	public virtual void OpenFriendSessionBrowser()
	{
		throw new NotImplementedException();
	}

	internal virtual bool ShouldEnableEnterGameButton()
	{
		return false;
	}

	internal virtual bool ShouldEnableFindFriendSessionButton()
	{
		return false;
	}

	public virtual bool ShouldShowSpinner()
	{
		return IsQuickPlaySearching();
	}

	public bool ShouldShowGenericSpinner()
	{
		return shouldShowGenericSpinner;
	}

	public void SetShouldShowGenericSpinner(bool shouldShow)
	{
		shouldShowGenericSpinner = shouldShow;
	}

	public virtual bool ShouldEnableInviteButton()
	{
		return CanInvite();
	}

	internal virtual void EnterGameButtonPressed()
	{
	}
}

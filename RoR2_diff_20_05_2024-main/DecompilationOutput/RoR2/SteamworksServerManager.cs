using System;
using System.Net;
using Facepunch.Steamworks;
using RoR2.ConVar;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace RoR2;

internal sealed class SteamworksServerManager : ServerManagerBase<SteamworksServerManager>, IDisposable
{
	private sealed class SteamServerHeartbeatEnabledConVar : BaseConVar
	{
		public static readonly SteamServerHeartbeatEnabledConVar instance = new SteamServerHeartbeatEnabledConVar("steam_server_heartbeat_enabled", ConVarFlags.Engine, null, "Whether or not this server issues any heartbeats to the Steam master server and effectively advertises it in the master server list. Default is 1 for dedicated servers, 0 for client builds.");

		public bool value { get; private set; }

		public SteamServerHeartbeatEnabledConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValueString)
		{
			if (!TextSerialization.TryParseInvariant(newValueString, out int result))
			{
				return;
			}
			bool flag = result != 0;
			if (flag != value)
			{
				value = flag;
				if (ServerManagerBase<SteamworksServerManager>.instance != null)
				{
					ServerManagerBase<SteamworksServerManager>.instance.steamworksServer.AutomaticHeartbeats = value;
				}
			}
		}

		public override string GetString()
		{
			if (!value)
			{
				return "0";
			}
			return "1";
		}
	}

	public class SteamServerPortConVar : BaseConVar
	{
		public ushort value { get; private set; }

		public SteamServerPortConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValueString)
		{
			if (NetworkServer.active)
			{
				throw new ConCommandException("Cannot change this convar while the server is running.");
			}
			if (TextSerialization.TryParseInvariant(newValueString, out ushort result))
			{
				value = result;
			}
		}

		public override string GetString()
		{
			return value.ToString();
		}
	}

	private Server steamworksServer;

	private IPAddress address;

	private static readonly SteamServerPortConVar cvSteamServerQueryPort = new SteamServerPortConVar("steam_server_query_port", ConVarFlags.Engine, "27016", "The port for queries.");

	private static readonly SteamServerPortConVar cvSteamServerSteamPort = new SteamServerPortConVar("steam_server_steam_port", ConVarFlags.Engine, "0", "The port for steam. 0 for a random port in the range 10000-60000.");

	public SteamworksServerManager()
	{
		string gameDesc = "Risk of Rain 2";
		steamworksServer = new Server(632360u, new ServerInit("Risk of Rain 2", gameDesc)
		{
			IpAddress = IPAddress.Any,
			Secure = true,
			VersionString = RoR2Application.GetBuildId(),
			GamePort = NetworkManagerSystem.SvPortConVar.instance.value,
			QueryPort = cvSteamServerQueryPort.value,
			SteamPort = cvSteamServerSteamPort.value,
			GameData = ServerManagerBase<SteamworksServerManager>.GetVersionGameDataString() + "," + NetworkModCompatibilityHelper.steamworksGameserverGameDataValue
		});
		Debug.LogFormat("steamworksServer.IsValid={0}", steamworksServer.IsValid);
		if (!steamworksServer.IsValid)
		{
			Dispose();
			return;
		}
		steamworksServer.Auth.OnAuthChange = OnAuthChange;
		steamworksServer.MaxPlayers = GetMaxPlayers();
		UpdateHostName(NetworkManagerSystem.SvHostNameConVar.instance.GetString());
		NetworkManagerSystem.SvHostNameConVar.instance.onValueChanged += UpdateHostName;
		UpdateMapName(SceneManager.GetActiveScene().name);
		NetworkManagerSystem.onServerSceneChangedGlobal += UpdateMapName;
		UpdatePassword(NetworkManagerSystem.SvPasswordConVar.instance.value);
		NetworkManagerSystem.SvPasswordConVar.instance.onValueChanged += UpdatePassword;
		steamworksServer.DedicatedServer = false;
		steamworksServer.AutomaticHeartbeats = SteamServerHeartbeatEnabledConVar.instance.value;
		steamworksServer.LogOnAnonymous();
		Debug.LogFormat("steamworksServer.LoggedOn={0}", steamworksServer.LoggedOn);
		RoR2Application.onUpdate += Update;
		NetworkManagerSystem.onServerConnectGlobal += OnServerConnectClient;
		NetworkManagerSystem.onServerDisconnectGlobal += OnServerDisconnectClient;
		ServerAuthManager.onAuthDataReceivedFromClient += OnAuthDataReceivedFromClient;
		ServerAuthManager.onAuthExpired += OnAuthExpired;
		Run.onServerRunSetRuleBookGlobal += base.OnServerRunSetRuleBookGlobal;
		PreGameController.onPreGameControllerSetRuleBookServerGlobal += base.OnPreGameControllerSetRuleBookServerGlobal;
		NetworkUser.onNetworkUserDiscovered += OnNetworkUserDiscovered;
		NetworkUser.onNetworkUserLost += OnNetworkUserLost;
		steamworksServer.SetKey("Test", "Value");
		steamworksServer.SetKey("gameMode", PreGameController.GameModeConVar.instance.GetString());
		steamworksServer.SetKey("buildId", RoR2Application.GetBuildId());
		steamworksServer.SetKey("modHash", NetworkModCompatibilityHelper.networkModHash);
		ruleBookKvHelper = new KeyValueSplitter("ruleBook", 2048, 2048, steamworksServer.SetKey);
		modListKvHelper = new KeyValueSplitter(NetworkModCompatibilityHelper.steamworksGameserverRulesBaseName, 2048, 2048, steamworksServer.SetKey);
		modListKvHelper.SetValue(NetworkModCompatibilityHelper.steamworksGameserverGameRulesValue);
		steamworksServer.ForceHeartbeat();
		UpdateServerRuleBook();
	}

	protected override void TagsStringUpdated()
	{
		base.TagsStringUpdated();
		steamworksServer.GameTags = base.tagsString;
	}

	private void OnAuthExpired(NetworkConnection conn, ClientAuthData authData)
	{
		SteamNetworkConnection obj = conn as SteamNetworkConnection;
		PlatformID? platformID = ((obj != null) ? new PlatformID?(obj.steamId) : authData?.platformId);
		if (platformID.HasValue)
		{
			steamworksServer.Auth.EndSession(platformID.Value.ID);
		}
	}

	private void OnAuthDataReceivedFromClient(NetworkConnection conn, ClientAuthData authData)
	{
		PlatformID platformID = authData.platformId;
		if (conn is SteamNetworkConnection steamNetworkConnection)
		{
			platformID = steamNetworkConnection.steamId;
		}
		steamworksServer.Auth.StartSession(authData.authTicket, platformID.ID);
	}

	private void OnServerConnectClient(NetworkConnection conn)
	{
	}

	private void OnServerDisconnectClient(NetworkConnection conn)
	{
	}

	public override void Dispose()
	{
		if (!disposed)
		{
			disposed = true;
			steamworksServer?.Dispose();
			steamworksServer = null;
			RoR2Application.onUpdate -= Update;
			NetworkManagerSystem.SvHostNameConVar.instance.onValueChanged -= UpdateHostName;
			NetworkManagerSystem.SvPasswordConVar.instance.onValueChanged -= UpdatePassword;
			NetworkManagerSystem.onServerSceneChangedGlobal -= UpdateMapName;
			NetworkManagerSystem.onServerConnectGlobal -= OnServerConnectClient;
			NetworkManagerSystem.onServerDisconnectGlobal -= OnServerDisconnectClient;
			ServerAuthManager.onAuthDataReceivedFromClient -= OnAuthDataReceivedFromClient;
			ServerAuthManager.onAuthExpired -= OnAuthExpired;
			Run.onServerRunSetRuleBookGlobal -= base.OnServerRunSetRuleBookGlobal;
			PreGameController.onPreGameControllerSetRuleBookServerGlobal -= base.OnPreGameControllerSetRuleBookServerGlobal;
			NetworkUser.onNetworkUserDiscovered -= OnNetworkUserDiscovered;
			NetworkUser.onNetworkUserLost -= OnNetworkUserLost;
		}
	}

	private int GetMaxPlayers()
	{
		return NetworkManagerSystem.singleton.maxConnections;
	}

	private void OnNetworkUserLost(NetworkUser networkUser)
	{
		UpdateBotPlayerCount();
	}

	private void OnNetworkUserDiscovered(NetworkUser networkUser)
	{
		UpdateBotPlayerCount();
	}

	private void UpdateBotPlayerCount()
	{
		int num = 0;
		foreach (NetworkUser readOnlyInstances in NetworkUser.readOnlyInstancesList)
		{
			if (readOnlyInstances.isSplitScreenExtraPlayer)
			{
				num++;
			}
		}
		steamworksServer.BotCount = num;
	}

	private void UpdateHostName(string newHostName)
	{
		steamworksServer.ServerName = newHostName;
	}

	private void UpdateMapName(string sceneName)
	{
		steamworksServer.MapName = sceneName;
	}

	private void UpdatePassword(string newPassword)
	{
		steamworksServer.Passworded = newPassword.Length > 0;
	}

	private void OnAddressDiscovered()
	{
	}

	private void RefreshSteamServerPlayers()
	{
		foreach (NetworkUser readOnlyInstances in NetworkUser.readOnlyInstancesList)
		{
			ClientAuthData clientAuthData = ServerAuthManager.FindAuthData(readOnlyInstances.connectionToClient);
			if (clientAuthData != null)
			{
				steamworksServer.UpdatePlayer(clientAuthData.platformId.ID, readOnlyInstances.userName, 0);
			}
		}
	}

	protected override void Update()
	{
		steamworksServer.Update();
		playerUpdateTimer -= Time.unscaledDeltaTime;
		if (playerUpdateTimer <= 0f)
		{
			playerUpdateTimer = playerUpdateInterval;
			RefreshSteamServerPlayers();
		}
		if (address == null)
		{
			address = steamworksServer.PublicIp;
			if (address != null)
			{
				OnAddressDiscovered();
			}
		}
	}

	private void OnAuthChange(ulong steamId, ulong ownerId, ServerAuth.Status status)
	{
		NetworkConnection networkConnection = ServerAuthManager.FindConnectionForSteamID(new PlatformID(steamId));
		if (networkConnection == null)
		{
			Debug.LogWarningFormat("SteamworksServerManager.OnAuthChange(steamId={0}, ownerId={1}, status={2}): Could not find connection for steamId.", steamId, ownerId, status);
			return;
		}
		switch (status)
		{
		case ServerAuth.Status.OK:
			break;
		default:
			throw new ArgumentOutOfRangeException("status", status, null);
		case ServerAuth.Status.UserNotConnectedToSteam:
		case ServerAuth.Status.NoLicenseOrExpired:
		case ServerAuth.Status.VACBanned:
		case ServerAuth.Status.LoggedInElseWhere:
		case ServerAuth.Status.VACCheckTimedOut:
		case ServerAuth.Status.AuthTicketCanceled:
		case ServerAuth.Status.AuthTicketInvalidAlreadyUsed:
		case ServerAuth.Status.AuthTicketInvalid:
		case ServerAuth.Status.PublisherIssuedBan:
			NetworkManagerSystem.singleton.ServerKickClient(networkConnection, new NetworkManagerSystem.SimpleLocalizedKickReason("KICK_REASON_STEAMWORKS_AUTH_FAILURE", status.ToString()));
			break;
		}
	}

	[ConCommand(commandName = "steam_server_force_heartbeat", flags = ConVarFlags.None, helpText = "Forces the server to issue a heartbeat to the master server.")]
	private static void CCSteamServerForceHeartbeat(ConCommandArgs args)
	{
		(ServerManagerBase<SteamworksServerManager>.instance?.steamworksServer ?? throw new ConCommandException("No Steamworks server is running.")).ForceHeartbeat();
	}

	[ConCommand(commandName = "steam_server_print_info", flags = ConVarFlags.None, helpText = "Prints debug info about the currently hosted Steamworks server.")]
	private static void CCSteamServerPrintInfo(ConCommandArgs args)
	{
		Server server = ServerManagerBase<SteamworksServerManager>.instance?.steamworksServer;
		if (server == null)
		{
			throw new ConCommandException("No Steamworks server is running.");
		}
		string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat("" + $"IsValid={server.IsValid}\n", $"Product={server.Product}\n"), $"ModDir={server.ModDir}\n"), $"SteamId={server.SteamId}\n"), $"DedicatedServer={server.DedicatedServer}\n"), $"LoggedOn={server.LoggedOn}\n"), $"ServerName={server.ServerName}\n"), $"PublicIp={server.PublicIp}\n"), $"Passworded={server.Passworded}\n"), $"MaxPlayers={server.MaxPlayers}\n"), $"BotCount={server.BotCount}\n"), $"MapName={server.MapName}\n"), $"GameDescription={server.GameDescription}\n"), $"GameTags={server.GameTags}\n");
	}
}

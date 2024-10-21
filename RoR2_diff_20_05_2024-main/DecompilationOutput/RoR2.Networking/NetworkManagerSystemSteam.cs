using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Facepunch.Steamworks;
using RoR2.ConVar;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Networking;

public class NetworkManagerSystemSteam : NetworkManagerSystem
{
	private BaseSteamworks clientSteamworks;

	private BaseSteamworks serverSteamworks;

	protected List<PlatformID> steamIdBanList = new List<PlatformID>();

	private static readonly BoolConVar cvSteamP2PUseSteamServer = new BoolConVar("steam_p2p_use_steam_server", ConVarFlags.None, "0", "Whether or not to use the Steam server interface to receive network traffic. Setting to false will cause the traffic to be handled by the Steam client interface instead. Only takes effect on server startup.");

	protected override void Start()
	{
		foreach (QosType channel in base.channels)
		{
			base.connectionConfig.AddChannel(channel);
		}
		base.connectionConfig.PacketSize = 1200;
		StartCoroutine(FireOnStartGlobalEvent());
	}

	protected override void Update()
	{
		UpdateTime(ref _unpredictedServerFrameTime, ref _unpredictedServerFrameTimeSmoothed, ref unpredictedServerFrameTimeVelocity, Time.deltaTime);
		EnsureDesiredHost();
		UpdateServer();
		UpdateClient();
	}

	protected override void EnsureDesiredHost()
	{
		if ((false | serverShuttingDown | base.clientIsConnecting | (NetworkServer.active && NetworkManagerSystem.isLoadingScene) | (!NetworkClient.active && NetworkManagerSystem.isLoadingScene)) || !SystemInitializerAttribute.hasExecuted)
		{
			return;
		}
		bool isAnyUserSignedIn = LocalUserManager.isAnyUserSignedIn;
		if (base.desiredHost.isRemote && !isAnyUserSignedIn)
		{
			return;
		}
		if (isNetworkActive && !actedUponDesiredHost && !base.desiredHost.DescribesCurrentHost())
		{
			Disconnect();
		}
		else
		{
			if (actedUponDesiredHost)
			{
				return;
			}
			if (base.desiredHost.hostType == HostDescription.HostType.Self)
			{
				if (NetworkServer.active)
				{
					return;
				}
				actedUponDesiredHost = true;
				base.maxConnections = base.desiredHost.hostingParameters.maxPlayers;
				NetworkServer.dontListen = !base.desiredHost.hostingParameters.listen;
				if (!isAnyUserSignedIn)
				{
					StartServer(base.connectionConfig, 4);
				}
				else
				{
					StartHost(base.connectionConfig, 4);
				}
			}
			if (base.desiredHost.hostType == HostDescription.HostType.Steam && Time.unscaledTime - lastDesiredHostSetTime >= 0f)
			{
				actedUponDesiredHost = true;
				StartClient(base.desiredHost.userID);
			}
			if (base.desiredHost.hostType == HostDescription.HostType.IPv4 && Time.unscaledTime - lastDesiredHostSetTime >= 0f)
			{
				actedUponDesiredHost = true;
				Debug.LogFormat("Attempting connection. ip={0} port={1}", base.desiredHost.addressPortPair.address, base.desiredHost.addressPortPair.port);
				NetworkManagerSystem.singleton.networkAddress = base.desiredHost.addressPortPair.address;
				NetworkManagerSystem.singleton.networkPort = base.desiredHost.addressPortPair.port;
				NetworkManagerSystem.singleton.StartClient();
			}
		}
	}

	public override void ForceCloseAllConnections()
	{
		Facepunch.Steamworks.Networking networking = Client.Instance?.Networking;
		if (networking == null)
		{
			return;
		}
		foreach (NetworkConnection connection in NetworkServer.connections)
		{
			if (connection is SteamNetworkConnection steamNetworkConnection)
			{
				networking.CloseSession(steamNetworkConnection.steamId.ID);
			}
		}
		if (client?.connection is SteamNetworkConnection steamNetworkConnection2)
		{
			networking.CloseSession(steamNetworkConnection2.steamId.ID);
		}
	}

	public override NetworkConnection GetClient(PlatformID clientID)
	{
		if (!NetworkServer.active)
		{
			return null;
		}
		if (clientID.ID == Client.Instance.SteamId && NetworkServer.connections.Count > 0)
		{
			return NetworkServer.connections[0];
		}
		foreach (NetworkConnection connection in NetworkServer.connections)
		{
			if (connection is SteamNetworkConnection { steamId: var steamId } steamNetworkConnection && steamId.value == clientID.value)
			{
				return steamNetworkConnection;
			}
		}
		Debug.LogError("Client not found");
		return null;
	}

	public override void OnServerConnect(NetworkConnection conn)
	{
		base.OnServerConnect(conn);
		if (NetworkUser.readOnlyInstancesList.Count >= base.maxConnections)
		{
			ServerKickClient(conn, new SimpleLocalizedKickReason("KICK_REASON_SERVERFULL"));
		}
		else
		{
			FireServerConnectGlobalEvent(conn);
		}
	}

	public override void OnServerDisconnect(NetworkConnection conn)
	{
		FireServerDisconnectGlobalEvent(conn);
		if (conn.clientOwnedObjects != null)
		{
			foreach (NetworkInstanceId item in new HashSet<NetworkInstanceId>(conn.clientOwnedObjects))
			{
				GameObject gameObject = NetworkServer.FindLocalObject(item);
				if (gameObject != null && (bool)gameObject.GetComponent<CharacterMaster>())
				{
					NetworkIdentity component = gameObject.GetComponent<NetworkIdentity>();
					if ((bool)component && component.clientAuthorityOwner == conn)
					{
						component.RemoveClientAuthority(conn);
					}
				}
			}
		}
		List<PlayerController> playerControllers = conn.playerControllers;
		for (int i = 0; i < playerControllers.Count; i++)
		{
			NetworkUser component2 = playerControllers[i].gameObject.GetComponent<NetworkUser>();
			if ((bool)component2)
			{
				Chat.SendPlayerDisconnectedMessage(component2);
			}
		}
		if (conn is SteamNetworkConnection)
		{
			Debug.LogFormat("Closing connection with steamId {0}", ((SteamNetworkConnection)conn).steamId);
		}
		base.OnServerDisconnect(conn);
	}

	public override void InitPlatformServer()
	{
		ServerManagerBase<SteamworksServerManager>.StartServer();
		InitP2P();
	}

	public override void OnStopServer()
	{
		FireStopServerGlobalEvent();
		for (int i = 0; i < NetworkServer.connections.Count; i++)
		{
			NetworkConnection networkConnection = NetworkServer.connections[i];
			if (networkConnection != null)
			{
				OnServerDisconnect(networkConnection);
			}
		}
		if ((bool)serverNetworkSessionInstance)
		{
			UnityEngine.Object.Destroy(serverNetworkSessionInstance);
		}
		serverNetworkSessionInstance = null;
		ServerManagerBase<SteamworksServerManager>.StopServer();
	}

	public override void ServerBanClient(NetworkConnection conn)
	{
		if (conn is SteamNetworkConnection steamNetworkConnection)
		{
			steamIdBanList.Add(steamNetworkConnection.steamId);
		}
	}

	protected override NetworkUserId AddPlayerIdFromPlatform(NetworkConnection conn, AddPlayerMessage message, byte playerControllerId)
	{
		NetworkUserId result = NetworkUserId.FromIp(conn.address, playerControllerId);
		PlatformID platformID = ServerAuthManager.FindAuthData(conn)?.platformId ?? PlatformID.nil;
		if (platformID != PlatformID.nil)
		{
			result = NetworkUserId.FromId(platformID.ID, playerControllerId);
		}
		return result;
	}

	protected override void KickClient(NetworkConnection conn, BaseKickReason reason)
	{
		if (conn is SteamNetworkConnection steamNetworkConnection)
		{
			steamNetworkConnection.ignore = true;
		}
	}

	public override void ServerHandleClientDisconnect(NetworkConnection conn)
	{
		OnServerDisconnect(conn);
		conn.InvokeHandlerNoData(33);
		conn.Disconnect();
		conn.Dispose();
		if (conn is SteamNetworkConnection)
		{
			NetworkServer.RemoveExternalConnection(conn.connectionId);
		}
	}

	protected override void UpdateServer()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		ReadOnlyCollection<NetworkConnection> connections = NetworkServer.connections;
		for (int num = connections.Count - 1; num >= 0; num--)
		{
			if (connections[num] is SteamNetworkConnection steamNetworkConnection)
			{
				Facepunch.Steamworks.Networking.P2PSessionState sessionState = default(Facepunch.Steamworks.Networking.P2PSessionState);
				if (Client.Instance.Networking.GetP2PSessionState(steamNetworkConnection.steamId.ID, ref sessionState) && sessionState.Connecting == 0 && sessionState.ConnectionActive == 0)
				{
					ServerHandleClientDisconnect(steamNetworkConnection);
				}
			}
		}
	}

	public override void OnStartClient(NetworkClient newClient)
	{
		base.OnStartClient(newClient);
		InitP2P();
	}

	public override void OnClientConnect(NetworkConnection conn)
	{
		base.OnClientConnect(conn);
	}

	public override void OnClientDisconnect(NetworkConnection conn)
	{
		if (conn is SteamNetworkConnection steamNetworkConnection)
		{
			Debug.LogFormat("Closing connection with steamId {0}", steamNetworkConnection.steamId);
		}
		base.OnClientDisconnect(conn);
	}

	protected override AddPlayerMessage CreateClientAddPlayerMessage()
	{
		if (Client.Instance != null)
		{
			return new AddPlayerMessage
			{
				id = new PlatformID(Client.Instance.SteamId),
				steamAuthTicketData = Client.Instance.Auth.GetAuthSessionTicket().Data
			};
		}
		return new AddPlayerMessage
		{
			id = default(PlatformID),
			steamAuthTicketData = Array.Empty<byte>()
		};
	}

	protected override void UpdateCheckInactiveConnections()
	{
		if (client?.connection is SteamNetworkConnection)
		{
			Facepunch.Steamworks.Networking.P2PSessionState sessionState = default(Facepunch.Steamworks.Networking.P2PSessionState);
			if (Client.Instance.Networking.GetP2PSessionState(((SteamNetworkConnection)client.connection).steamId.ID, ref sessionState) && sessionState.Connecting == 0 && sessionState.ConnectionActive == 0)
			{
				client.connection.InvokeHandlerNoData(33);
				StopClient();
			}
		}
	}

	protected override void PlatformAuth(ref ClientAuthData authData, NetworkConnection conn)
	{
		authData.platformId = new PlatformID(Client.Instance.SteamId);
		authData.authTicket = Client.Instance.Auth.GetAuthSessionTicket().Data;
		authData.entitlements = PlatformSystems.entitlementsSystem.BuildEntitlements();
	}

	protected override void StartClient(PlatformID serverID)
	{
		if (!NetworkServer.active)
		{
			NetworkManager.networkSceneName = "";
		}
		string text = "";
		if (isNetworkActive)
		{
			text += "isNetworkActive ";
		}
		if (NetworkClient.active)
		{
			text += "NetworkClient.active ";
		}
		if (NetworkServer.active)
		{
			text += "NetworkClient.active ";
		}
		if (NetworkManagerSystem.isLoadingScene)
		{
			text += "isLoadingScene ";
		}
		if (text != "")
		{
			RoR2Application.onNextUpdate += delegate
			{
			};
		}
		SteamNetworkConnection steamNetworkConnection = new SteamNetworkConnection(Client.Instance, serverID);
		SteamNetworkClient steamNetworkClient = new SteamNetworkClient(steamNetworkConnection);
		steamNetworkClient.Configure(base.connectionConfig, 1);
		UseExternalClient(steamNetworkClient);
		steamNetworkClient.Connect();
		Debug.LogFormat("Initiating connection to server {0}...", serverID.value);
		if (!steamNetworkConnection.SendConnectionRequest())
		{
			Debug.LogFormat("Failed to send connection request to server {0}.", serverID.value);
		}
	}

	public override bool IsConnectedToServer(PlatformID serverID)
	{
		if (client == null || !client.connection.isConnected || Client.Instance == null)
		{
			return false;
		}
		if (client.connection is SteamNetworkConnection steamNetworkConnection)
		{
			return steamNetworkConnection.steamId == serverID;
		}
		if (client.connection.address == "localServer")
		{
			return serverID == base.serverP2PId;
		}
		return false;
	}

	public static bool IsMemberInSteamLobby(PlatformID steamId)
	{
		return Client.Instance.Lobby.UserIsInCurrentLobby(steamId.ID);
	}

	private static PlatformID GetSteamworksSteamId(BaseSteamworks steamworks)
	{
		if (steamworks is Client client)
		{
			return new PlatformID(client.SteamId);
		}
		if (steamworks is Server server)
		{
			return new PlatformID(server.SteamId);
		}
		return PlatformID.nil;
	}

	protected void InitP2P()
	{
		Facepunch.Steamworks.Networking networking = Server.Instance?.Networking;
		Facepunch.Steamworks.Networking networking2 = Client.Instance?.Networking;
		if (networking != null)
		{
			networking.OnIncomingConnection = OnSteamServerP2PIncomingConnection;
			networking.OnConnectionFailed = OnSteamServerP2PConnectionFailed;
			networking.OnP2PData = OnSteamServerP2PData;
			for (int i = 0; i < base.connectionConfig.ChannelCount; i++)
			{
				networking.SetListenChannel(i, Listen: true);
			}
		}
		if (networking2 != null)
		{
			networking2.OnIncomingConnection = OnSteamClientP2PIncomingConnection;
			networking2.OnConnectionFailed = OnSteamClientP2PConnectionFailed;
			networking2.OnP2PData = OnSteamClientP2PData;
			for (int j = 0; j < base.connectionConfig.ChannelCount; j++)
			{
				networking2.SetListenChannel(j, Listen: true);
			}
		}
		clientSteamworks = Client.Instance;
		serverSteamworks = null;
		if (NetworkServer.active)
		{
			serverSteamworks = (BaseSteamworks)(((object)(cvSteamP2PUseSteamServer.value ? Server.Instance : null)) ?? ((object)Client.Instance));
		}
		base.clientP2PId = GetSteamworksSteamId(clientSteamworks);
		base.serverP2PId = GetSteamworksSteamId(serverSteamworks);
	}

	private bool OnSteamServerP2PIncomingConnection(ulong senderSteamId)
	{
		return OnIncomingP2PConnection(Server.Instance, new PlatformID(senderSteamId));
	}

	private void OnSteamServerP2PConnectionFailed(ulong senderSteamId, Facepunch.Steamworks.Networking.SessionError sessionError)
	{
		OnP2PConnectionFailed(Server.Instance, new PlatformID(senderSteamId), sessionError);
	}

	private void OnSteamServerP2PData(ulong senderSteamId, byte[] data, int dataLength, int channel)
	{
		OnP2PData(Server.Instance, new PlatformID(senderSteamId), data, dataLength, channel);
	}

	private bool OnSteamClientP2PIncomingConnection(ulong senderSteamId)
	{
		return OnIncomingP2PConnection(Client.Instance, new PlatformID(senderSteamId));
	}

	private void OnSteamClientP2PConnectionFailed(ulong senderSteamId, Facepunch.Steamworks.Networking.SessionError sessionError)
	{
		OnP2PConnectionFailed(Client.Instance, new PlatformID(senderSteamId), sessionError);
	}

	private void OnSteamClientP2PData(ulong senderSteamId, byte[] data, int dataLength, int channel)
	{
		OnP2PData(Client.Instance, new PlatformID(senderSteamId), data, dataLength, channel);
	}

	private bool OnIncomingP2PConnection(BaseSteamworks receiver, PlatformID senderSteamId)
	{
		bool flag = false;
		if (receiver == serverSteamworks)
		{
			if (NetworkServer.active)
			{
				flag = !NetworkServer.dontListen && !steamIdBanList.Contains(senderSteamId) && !IsServerAtMaxConnections();
			}
			else if (client is SteamNetworkClient && ((SteamNetworkClient)client).steamConnection.steamId == senderSteamId)
			{
				flag = true;
			}
		}
		Debug.LogFormat("Incoming Steamworks connection from Steam ID {0}: {1}", senderSteamId, flag ? "accepted" : "rejected");
		if (flag)
		{
			CreateServerP2PConnectionWithPeer(receiver, senderSteamId);
		}
		return flag;
	}

	private void OnP2PConnectionFailed(BaseSteamworks receiver, PlatformID senderSteamId, Facepunch.Steamworks.Networking.SessionError sessionError)
	{
		Debug.LogFormat("NetworkManagerSystem.OnClientP2PConnectionFailed steamId={0} sessionError={1}", senderSteamId, sessionError);
		SteamNetworkConnection steamNetworkConnection = SteamNetworkConnection.Find(receiver, senderSteamId);
		if (steamNetworkConnection != null)
		{
			if (client != null && client.connection == steamNetworkConnection)
			{
				steamNetworkConnection.InvokeHandlerNoData(33);
				steamNetworkConnection.Disconnect();
				steamNetworkConnection.Dispose();
			}
			if (NetworkServer.active && NetworkServer.connections.IndexOf(steamNetworkConnection) != -1)
			{
				ServerHandleClientDisconnect(steamNetworkConnection);
			}
		}
	}

	private void OnP2PData(BaseSteamworks receiver, PlatformID senderSteamId, byte[] data, int dataLength, int channel)
	{
		if (SteamNetworkConnection.cvNetP2PDebugTransport.value)
		{
			Debug.LogFormat("Received packet from {0} dataLength={1} channel={2}", senderSteamId, dataLength, channel);
		}
		SteamNetworkConnection steamNetworkConnection = SteamNetworkConnection.Find(receiver, senderSteamId);
		if (steamNetworkConnection != null)
		{
			steamNetworkConnection.TransportReceive(data, dataLength, 0);
			return;
		}
		Debug.LogFormat("Rejecting data from sender: Not associated with a registered connection. steamid={0} dataLength={1}", senderSteamId, data);
	}

	public void CreateServerP2PConnectionWithPeer(BaseSteamworks steamworks, PlatformID peer)
	{
		SteamNetworkConnection steamNetworkConnection = new SteamNetworkConnection(steamworks, peer);
		steamNetworkConnection.ForceInitialize(NetworkServer.hostTopology);
		int num = -1;
		ReadOnlyCollection<NetworkConnection> connections = NetworkServer.connections;
		for (int i = 1; i < connections.Count; i++)
		{
			if (connections[i] == null)
			{
				num = i;
				break;
			}
		}
		if (num == -1)
		{
			num = connections.Count;
		}
		steamNetworkConnection.connectionId = num;
		NetworkServer.AddExternalConnection(steamNetworkConnection);
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.StartMessage(32);
		networkWriter.FinishMessage();
		steamNetworkConnection.SendWriter(networkWriter, QosChannelIndex.defaultReliable.intVal);
	}

	protected override void PlatformClientSetPlayers(ConCommandArgs args)
	{
		if (client != null && client.connection != null)
		{
			ClientSetPlayers(client.connection);
		}
	}

	protected override void PlatformConnectP2P(ConCommandArgs args)
	{
		CheckSteamworks();
		PlatformID argPlatformID = args.GetArgPlatformID(0);
		if (Client.Instance.Lobby.IsValid && !PlatformSystems.lobbyManager.ownsLobby && argPlatformID != PlatformSystems.lobbyManager.newestLobbyData.serverId)
		{
			Debug.LogFormat("Cannot connect to server {0}: Server is not the one specified by the current steam lobby.", argPlatformID);
		}
		else if (!(base.clientP2PId == argPlatformID))
		{
			NetworkManagerSystem.singleton.desiredHost = new HostDescription(argPlatformID, HostDescription.HostType.Steam);
		}
	}

	protected override void PlatformDisconnect(ConCommandArgs args)
	{
		NetworkManagerSystem.singleton.desiredHost = HostDescription.none;
	}

	protected override void PlatformConnect(ConCommandArgs args)
	{
		AddressPortPair argAddressPortPair = args.GetArgAddressPortPair(0);
		if ((bool)NetworkManagerSystem.singleton)
		{
			NetworkManagerSystem.EnsureNetworkManagerNotBusy();
			Debug.LogFormat("Parsed address={0} port={1}. Setting desired host.", argAddressPortPair.address, argAddressPortPair.port);
			NetworkManagerSystem.singleton.desiredHost = new HostDescription(argAddressPortPair);
		}
	}

	protected override void PlatformHost(ConCommandArgs args)
	{
		if (!NetworkManagerSystem.singleton)
		{
			return;
		}
		bool argBool = args.GetArgBool(0);
		if (PlatformSystems.lobbyManager.isInLobby && !PlatformSystems.lobbyManager.ownsLobby)
		{
			return;
		}
		bool flag = false;
		if (NetworkServer.active)
		{
			flag = true;
		}
		if (!flag)
		{
			int maxPlayers = SvMaxPlayersConVar.instance.intValue;
			if (PlatformSystems.lobbyManager.isInLobby)
			{
				maxPlayers = PlatformSystems.lobbyManager.newestLobbyData.totalMaxPlayers;
			}
			NetworkManagerSystem.singleton.desiredHost = new HostDescription(new HostDescription.HostingParameters
			{
				listen = argBool,
				maxPlayers = maxPlayers
			});
		}
	}

	protected override void PlatformGetP2PSessionState(ConCommandArgs args)
	{
		CheckSteamworks();
		PlatformID argPlatformID = args.GetArgPlatformID(0);
		if ((bool)NetworkManagerSystem.singleton)
		{
			Facepunch.Steamworks.Networking.P2PSessionState sessionState = default(Facepunch.Steamworks.Networking.P2PSessionState);
			if (Client.Instance.Networking.GetP2PSessionState(argPlatformID.ID, ref sessionState))
			{
				Debug.LogFormat("ConnectionActive={0}\nConnecting={1}\nP2PSessionError={2}\nUsingRelay={3}\nBytesQueuedForSend={4}\nPacketsQueuedForSend={5}\nRemoteIP={6}\nRemotePort={7}", sessionState.ConnectionActive, sessionState.Connecting, sessionState.P2PSessionError, sessionState.UsingRelay, sessionState.BytesQueuedForSend, sessionState.PacketsQueuedForSend, sessionState.RemoteIP, sessionState.RemotePort);
			}
			else
			{
				Debug.LogFormat("Could not get p2p session info for steamId={0}", argPlatformID);
			}
		}
	}

	protected override void PlatformKick(ConCommandArgs args)
	{
		CheckSteamworks();
		PlatformID argPlatformID = args.GetArgPlatformID(0);
		NetworkConnection networkConnection = NetworkManagerSystem.singleton.GetClient(argPlatformID);
		if (networkConnection != null)
		{
			NetworkManagerSystem.singleton.ServerKickClient(networkConnection, new SimpleLocalizedKickReason("KICK_REASON_KICK"));
		}
	}

	protected override void PlatformBan(ConCommandArgs args)
	{
		CheckSteamworks();
		PlatformID argPlatformID = args.GetArgPlatformID(0);
		NetworkConnection networkConnection = NetworkManagerSystem.singleton.GetClient(argPlatformID);
		if (networkConnection != null)
		{
			NetworkManagerSystem.singleton.ServerBanClient(networkConnection);
			NetworkManagerSystem.singleton.ServerKickClient(networkConnection, new SimpleLocalizedKickReason("KICK_REASON_BAN"));
		}
	}

	public static void CheckSteamworks()
	{
		if (Client.Instance == null)
		{
			throw new ConCommandException("Steamworks not available.");
		}
	}

	public override void CreateLocalLobby()
	{
	}
}

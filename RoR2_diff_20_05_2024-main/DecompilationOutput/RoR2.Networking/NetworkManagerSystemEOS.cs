using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Epic.OnlineServices;
using Epic.OnlineServices.Lobby;
using Epic.OnlineServices.P2P;
using Facepunch.Steamworks;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Networking;

public class NetworkManagerSystemEOS : NetworkManagerSystem
{
	public ProductUserId myUserId;

	private int packetsRecieved;

	private Dictionary<ulong, byte> sequenceNumber = new Dictionary<ulong, byte>();

	private Dictionary<ulong, List<Tuple<byte, byte[], uint>>> outOfOrderPackets = new Dictionary<ulong, List<Tuple<byte, byte[], uint>>>();

	private List<ProductUserId> epicIdBanList = new List<ProductUserId>();

	public static SocketId socketId = new SocketId
	{
		SocketName = "RoR2EOS"
	};

	public static P2PInterface P2pInterface { get; private set; }

	protected override void Start()
	{
		myUserId = null;
		foreach (QosType channel in base.channels)
		{
			base.connectionConfig.AddChannel(channel);
		}
		base.connectionConfig.PacketSize = 1200;
		StartCoroutine(FireOnStartGlobalEvent());
	}

	protected override void Update()
	{
		if (!(EOSLoginManager.loggedInProductId == null))
		{
			UpdateTime(ref _unpredictedServerFrameTime, ref _unpredictedServerFrameTimeSmoothed, ref unpredictedServerFrameTimeVelocity, Time.deltaTime);
			EnsureDesiredHost();
			UpdateServer();
			UpdateClient();
			UpdateNetworkReceiveLoop();
		}
	}

	public void ResetSequencing()
	{
		packetsRecieved = 0;
		sequenceNumber.Clear();
		outOfOrderPackets.Clear();
	}

	private void UpdateNetworkReceiveLoop()
	{
		if (!(P2pInterface == null))
		{
			byte[] array = new byte[base.connectionConfig.PacketSize];
			ArraySegment<byte> arraySegment = new ArraySegment<byte>(array);
			int num = 0;
			bool flag = true;
			while (flag)
			{
				ReceivePacketOptions receivePacketOptions = default(ReceivePacketOptions);
				receivePacketOptions.LocalUserId = EOSLoginManager.loggedInProductId;
				receivePacketOptions.MaxDataSizeBytes = base.connectionConfig.PacketSize;
				ReceivePacketOptions options = receivePacketOptions;
				num++;
				ProductUserId outPeerId;
				SocketId outSocketId;
				byte outChannel;
				uint outBytesWritten;
				Result result = P2pInterface.ReceivePacket(ref options, out outPeerId, out outSocketId, out outChannel, arraySegment, out outBytesWritten);
				flag = ProcessData(result, outPeerId, arraySegment, (int)outBytesWritten, outChannel);
			}
		}
	}

	private bool ProcessData(Result result, ProductUserId incomingUserId, ArraySegment<byte> buf, int outBytesWritten, int channelId)
	{
		switch (result)
		{
		case Result.Success:
		{
			EOSNetworkConnection eOSNetworkConnection = EOSNetworkConnection.Find(myUserId, incomingUserId);
			if (eOSNetworkConnection != null)
			{
				eOSNetworkConnection.TransportReceive(buf.Array, outBytesWritten, 0);
			}
			else
			{
				Debug.LogFormat("Rejecting data from sender: Not associated with a registered connection. id={0} dataLength={1}", incomingUserId, buf.Count);
			}
			return true;
		}
		case Result.InvalidParameters:
		case Result.NotFound:
			return false;
		default:
			Debug.LogError("P2PInterface ReceivePacket returned a failure: " + result);
			return false;
		}
	}

	protected override void EnsureDesiredHost()
	{
		if (false | serverShuttingDown | base.clientIsConnecting | (NetworkServer.active && NetworkManagerSystem.isLoadingScene) | (!NetworkClient.active && NetworkManagerSystem.isLoadingScene))
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
			int maxPlayers = RoR2Application.maxPlayers;
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
					StartServer(base.connectionConfig, maxPlayers);
				}
				else
				{
					StartHost(base.connectionConfig, maxPlayers);
				}
			}
			if (base.desiredHost.hostType == HostDescription.HostType.EOS && Time.unscaledTime - lastDesiredHostSetTime >= 0f)
			{
				actedUponDesiredHost = true;
				StartClient(base.desiredHost.userID.productUserID);
			}
			if (base.desiredHost.hostType == HostDescription.HostType.IPv4 && Time.unscaledTime - lastDesiredHostSetTime >= 0f)
			{
				actedUponDesiredHost = true;
				Debug.LogFormat("Attempting connection. ip={0} port={1}", base.desiredHost.addressPortPair.address, base.desiredHost.addressPortPair.port);
				NetworkManagerSystem.singleton.networkAddress = base.desiredHost.addressPortPair.address;
				NetworkManagerSystem.singleton.networkPort = base.desiredHost.addressPortPair.port;
				NetworkManagerSystem.singleton.StartClient(matchInfo, base.connectionConfig);
			}
		}
	}

	public override void ForceCloseAllConnections()
	{
		foreach (NetworkConnection connection in NetworkServer.connections)
		{
			if (connection is EOSNetworkConnection eOSNetworkConnection)
			{
				CloseConnectionOptions closeConnectionOptions = default(CloseConnectionOptions);
				closeConnectionOptions.LocalUserId = eOSNetworkConnection.LocalUserID;
				closeConnectionOptions.RemoteUserId = eOSNetworkConnection.RemoteUserID;
				closeConnectionOptions.SocketId = socketId;
				CloseConnectionOptions options = closeConnectionOptions;
				P2pInterface.CloseConnection(ref options);
			}
		}
		if (client?.connection is EOSNetworkConnection eOSNetworkConnection2)
		{
			CloseConnectionOptions closeConnectionOptions = default(CloseConnectionOptions);
			closeConnectionOptions.LocalUserId = eOSNetworkConnection2.LocalUserID;
			closeConnectionOptions.RemoteUserId = eOSNetworkConnection2.RemoteUserID;
			closeConnectionOptions.SocketId = socketId;
			CloseConnectionOptions options2 = closeConnectionOptions;
			P2pInterface.CloseConnection(ref options2);
		}
		myUserId = null;
	}

	public override NetworkConnection GetClient(PlatformID clientID)
	{
		if (!NetworkServer.active)
		{
			return null;
		}
		if (clientID.productUserID == myUserId && NetworkServer.connections.Count > 0)
		{
			return NetworkServer.connections[0];
		}
		foreach (NetworkConnection connection in NetworkServer.connections)
		{
			if (connection is EOSNetworkConnection eOSNetworkConnection && eOSNetworkConnection.RemoteUserID == clientID.productUserID)
			{
				return eOSNetworkConnection;
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
		if (conn == null)
		{
			return;
		}
		FireServerDisconnectGlobalEvent(conn);
		if (conn?.clientOwnedObjects != null)
		{
			foreach (NetworkInstanceId item in new HashSet<NetworkInstanceId>(conn.clientOwnedObjects))
			{
				if (item.IsEmpty())
				{
					break;
				}
				GameObject gameObject = NetworkServer.FindLocalObject(item);
				if (gameObject != null && (bool)gameObject.GetComponent<CharacterMaster>() && gameObject.TryGetComponent<NetworkIdentity>(out var component) && component.clientAuthorityOwner == conn)
				{
					component.RemoveClientAuthority(conn);
				}
			}
		}
		List<PlayerController> list = conn?.playerControllers;
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i]?.gameObject != null && list[i].gameObject.TryGetComponent<NetworkUser>(out var component2))
			{
				Chat.SendPlayerDisconnectedMessage(component2);
			}
		}
		if (conn is EOSNetworkConnection eOSNetworkConnection)
		{
			_ = eOSNetworkConnection.RemoteUserID != null;
		}
		base.OnServerDisconnect(conn);
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		ServerManagerBase<EOSServerManager>.StartServer();
		InitP2P();
		NetworkMessageHandlerAttribute.RegisterServerMessages();
		InitializeTime();
		if (NetworkManager.singleton == null)
		{
			serverNetworkSessionInstance = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkSession"));
		}
		else
		{
			serverNetworkSessionInstance = NetworkManager.singleton.gameObject;
		}
		FireStartServerGlobalEvent();
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
		serverNetworkSessionInstance = null;
		ServerManagerBase<EOSServerManager>.StopServer();
		myUserId = null;
		base.OnStopServer();
	}

	public override void ServerBanClient(NetworkConnection conn)
	{
		if (conn is EOSNetworkConnection eOSNetworkConnection)
		{
			epicIdBanList.Add(eOSNetworkConnection.RemoteUserID);
		}
	}

	protected override NetworkUserId AddPlayerIdFromPlatform(NetworkConnection conn, AddPlayerMessage message, byte playerControllerId)
	{
		NetworkUserId result = NetworkUserId.FromIp(conn.address, playerControllerId);
		PlatformID? platformID = ServerAuthManager.FindAuthData(conn)?.platformId;
		if (platformID.HasValue)
		{
			if (platformID.Value.isSteam)
			{
				Debug.LogError("THIS SHOULD NOT HAPPEN INSIDE AN EOS GAME!");
			}
			result = new NetworkUserId(platformID.Value.stringID, playerControllerId);
		}
		return result;
	}

	protected override void KickClient(NetworkConnection conn, BaseKickReason reason)
	{
		if (conn is EOSNetworkConnection eOSNetworkConnection)
		{
			eOSNetworkConnection.ignore = true;
		}
	}

	public override void ServerHandleClientDisconnect(NetworkConnection conn)
	{
		OnServerDisconnect(conn);
		conn.InvokeHandlerNoData(33);
		conn.Disconnect();
		conn.Dispose();
		if (conn is EOSNetworkConnection)
		{
			NetworkServer.RemoveExternalConnection(conn.connectionId);
		}
	}

	protected override void UpdateServer()
	{
	}

	public override void OnStartClient(NetworkClient newClient)
	{
		base.OnStartClient(newClient);
		InitP2P();
	}

	public override void OnClientDisconnect(NetworkConnection conn)
	{
		if (conn is EOSNetworkConnection eOSNetworkConnection)
		{
			Debug.LogFormat("Closing connection with remote ID: {0}", eOSNetworkConnection.RemoteUserID);
		}
		base.OnClientDisconnect(conn);
	}

	protected override AddPlayerMessage CreateClientAddPlayerMessage()
	{
		if (client != null)
		{
			return new AddPlayerMessage
			{
				id = new PlatformID(EOSLoginManager.loggedInProductId),
				steamAuthTicketData = Array.Empty<byte>()
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
	}

	protected override void StartClient(PlatformID serverID)
	{
		StartClient(serverID.productUserID);
	}

	protected override void PlatformAuth(ref ClientAuthData authData, NetworkConnection conn)
	{
		authData.platformId = new PlatformID(EOSLoginManager.loggedInProductId);
		authData.entitlements = PlatformSystems.entitlementsSystem.BuildEntitlements();
		authData.authTicket = Client.Instance.Auth.GetAuthSessionTicket().Data;
	}

	private void StartClient(ProductUserId remoteUserId)
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
		EOSNetworkConnection eOSNetworkConnection = new EOSNetworkConnection(EOSLoginManager.loggedInProductId, remoteUserId);
		EOSNetworkClient eOSNetworkClient = new EOSNetworkClient(eOSNetworkConnection);
		eOSNetworkClient.Configure(base.connectionConfig, 1);
		UseExternalClient(eOSNetworkClient);
		eOSNetworkClient.Connect();
		Debug.LogFormat("Initiating connection to server {0}...", remoteUserId);
		if (!eOSNetworkConnection.SendConnectionRequest())
		{
			Debug.LogFormat("Failed to send connection request to server {0}.", remoteUserId);
		}
	}

	public override bool IsConnectedToServer(PlatformID serverID)
	{
		if (client == null || !client.connection.isConnected)
		{
			return false;
		}
		if (client.connection is EOSNetworkConnection eOSNetworkConnection)
		{
			return eOSNetworkConnection.RemoteUserID == serverID.productUserID;
		}
		if (client.connection.address == "localServer")
		{
			return serverID == base.serverP2PId;
		}
		return false;
	}

	private void OnConnectionRequested(ref OnIncomingConnectionRequestInfo connectionRequestInfo)
	{
		bool flag = false;
		if (connectionRequestInfo.SocketId.Value.SocketName == socketId.SocketName)
		{
			if (NetworkServer.active)
			{
				flag = !NetworkServer.dontListen && !epicIdBanList.Contains(connectionRequestInfo.RemoteUserId) && !IsServerAtMaxConnections();
			}
			else if (client is EOSNetworkClient eOSNetworkClient && eOSNetworkClient.eosConnection.RemoteUserID == connectionRequestInfo.RemoteUserId)
			{
				flag = true;
			}
		}
		if (flag)
		{
			AcceptConnectionOptions acceptConnectionOptions = default(AcceptConnectionOptions);
			acceptConnectionOptions.LocalUserId = connectionRequestInfo.LocalUserId;
			acceptConnectionOptions.RemoteUserId = connectionRequestInfo.RemoteUserId;
			acceptConnectionOptions.SocketId = socketId;
			AcceptConnectionOptions options = acceptConnectionOptions;
			P2pInterface.AcceptConnection(ref options);
			CreateServerP2PConnectionWithPeer(connectionRequestInfo.RemoteUserId);
		}
	}

	public void CreateServerP2PConnectionWithPeer(ProductUserId peer)
	{
		EOSNetworkConnection eOSNetworkConnection = new EOSNetworkConnection(myUserId, peer);
		eOSNetworkConnection.ForceInitialize(NetworkServer.hostTopology);
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
		eOSNetworkConnection.connectionId = num;
		NetworkServer.AddExternalConnection(eOSNetworkConnection);
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.StartMessage(32);
		networkWriter.FinishMessage();
		eOSNetworkConnection.SendWriter(networkWriter, QosChannelIndex.defaultReliable.intVal);
	}

	protected void InitP2P()
	{
		if (P2pInterface == null)
		{
			P2pInterface = EOSPlatformManager.GetPlatformInterface().GetP2PInterface();
			AddNotifyPeerConnectionRequestOptions addNotifyPeerConnectionRequestOptions = default(AddNotifyPeerConnectionRequestOptions);
			addNotifyPeerConnectionRequestOptions.LocalUserId = EOSLoginManager.loggedInProductId;
			addNotifyPeerConnectionRequestOptions.SocketId = socketId;
			AddNotifyPeerConnectionRequestOptions options = addNotifyPeerConnectionRequestOptions;
			P2pInterface.AddNotifyPeerConnectionRequest(ref options, null, OnConnectionRequested);
			AddNotifyPeerConnectionClosedOptions addNotifyPeerConnectionClosedOptions = default(AddNotifyPeerConnectionClosedOptions);
			addNotifyPeerConnectionClosedOptions.LocalUserId = EOSLoginManager.loggedInProductId;
			addNotifyPeerConnectionClosedOptions.SocketId = socketId;
			AddNotifyPeerConnectionClosedOptions options2 = addNotifyPeerConnectionClosedOptions;
			P2pInterface.AddNotifyPeerConnectionClosed(ref options2, null, OnConnectionClosed);
		}
		myUserId = EOSLoginManager.loggedInProductId;
		base.serverP2PId = new PlatformID(myUserId);
	}

	private void OnConnectionClosed(ref OnRemoteConnectionClosedInfo connectionRequestInfo)
	{
		EOSNetworkConnection eOSNetworkConnection = EOSNetworkConnection.Find(myUserId, connectionRequestInfo.RemoteUserId);
		if (client != null && client.connection == eOSNetworkConnection)
		{
			eOSNetworkConnection.InvokeHandlerNoData(33);
			eOSNetworkConnection.Disconnect();
			eOSNetworkConnection.Dispose();
		}
		if (NetworkServer.active && NetworkServer.connections.IndexOf(eOSNetworkConnection) != -1)
		{
			ServerHandleClientDisconnect(eOSNetworkConnection);
		}
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
				LobbyDetailsCopyInfoOptions options = default(LobbyDetailsCopyInfoOptions);
				maxPlayers = (int)(((PlatformSystems.lobbyManager as EOSLobbyManager).CurrentLobbyDetails.CopyInfo(ref options, out var outLobbyDetailsInfo) == Result.Success) ? outLobbyDetailsInfo.Value.MaxMembers : 0);
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
		args.GetArgPlatformID(0);
		_ = (bool)NetworkManagerSystem.singleton;
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

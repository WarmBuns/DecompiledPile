using System;
using System.Collections.Generic;
using System.Linq;
using Epic.OnlineServices;
using Epic.OnlineServices.P2P;
using RoR2.ConVar;
using UnityEngine.Networking;

namespace RoR2.Networking;

public class EOSNetworkConnection : NetworkConnection
{
	private static List<EOSNetworkConnection> instancesList = new List<EOSNetworkConnection>();

	public bool ignore;

	public static BoolConVar cvNetP2PDebugTransport = new BoolConVar("net_p2p_debug_transport", ConVarFlags.None, "0", "Allows p2p transport information to print to the console.");

	private static BoolConVar cvNetP2PLogMessages = new BoolConVar("net_p2p_log_messages", ConVarFlags.None, "0", "Enables logging of network messages.");

	public ProductUserId LocalUserID { get; }

	public ProductUserId RemoteUserID { get; private set; }

	public EOSNetworkConnection()
	{
	}

	public EOSNetworkConnection(ProductUserId localUserID, ProductUserId remoteUserID)
	{
		LocalUserID = localUserID;
		RemoteUserID = remoteUserID;
		instancesList.Add(this);
	}

	public bool SendConnectionRequest()
	{
		SendPacketOptions sendPacketOptions = default(SendPacketOptions);
		sendPacketOptions.LocalUserId = LocalUserID;
		sendPacketOptions.RemoteUserId = RemoteUserID;
		sendPacketOptions.AllowDelayedDelivery = true;
		sendPacketOptions.Channel = 0;
		sendPacketOptions.Reliability = PacketReliability.ReliableOrdered;
		sendPacketOptions.SocketId = NetworkManagerSystemEOS.socketId;
		SendPacketOptions options = sendPacketOptions;
		return NetworkManagerSystemEOS.P2pInterface.SendPacket(ref options) == Result.Success;
	}

	public override bool TransportSend(byte[] bytes, int numBytes, int channelId, out byte error)
	{
		if (ignore)
		{
			error = 0;
			return true;
		}
		logNetworkMessages = cvNetP2PLogMessages.value;
		if (LocalUserID == RemoteUserID)
		{
			TransportReceive(bytes, numBytes, channelId);
			error = 0;
			_ = cvNetP2PDebugTransport.value;
			return true;
		}
		QosType qOS = NetworkManagerSystem.singleton.connectionConfig.Channels[channelId].QOS;
		PacketReliability packetReliability = PacketReliability.ReliableOrdered;
		packetReliability = ((qOS != 0 && qOS != QosType.UnreliableFragmented && qOS != QosType.UnreliableSequenced) ? PacketReliability.ReliableOrdered : PacketReliability.UnreliableUnordered);
		ArraySegment<byte> data = new ArraySegment<byte>(bytes, 0, numBytes);
		SendPacketOptions sendPacketOptions = default(SendPacketOptions);
		sendPacketOptions.AllowDelayedDelivery = true;
		sendPacketOptions.Data = data;
		sendPacketOptions.Channel = 0;
		sendPacketOptions.LocalUserId = LocalUserID;
		sendPacketOptions.RemoteUserId = RemoteUserID;
		sendPacketOptions.SocketId = NetworkManagerSystemEOS.socketId;
		sendPacketOptions.Reliability = packetReliability;
		SendPacketOptions options = sendPacketOptions;
		if (NetworkManagerSystemEOS.P2pInterface.SendPacket(ref options) == Result.Success)
		{
			error = 0;
			return true;
		}
		error = 1;
		return false;
	}

	public override void TransportReceive(byte[] bytes, int numBytes, int channelId)
	{
		if (!ignore)
		{
			logNetworkMessages = cvNetP2PLogMessages.value;
			base.TransportReceive(bytes, numBytes, channelId);
		}
	}

	protected override void Dispose(bool disposing)
	{
		instancesList.Remove(this);
		P2PInterface p2PInterface = EOSPlatformManager.GetPlatformInterface().GetP2PInterface();
		if (p2PInterface != null)
		{
			CloseConnectionOptions closeConnectionOptions = default(CloseConnectionOptions);
			closeConnectionOptions.LocalUserId = LocalUserID;
			closeConnectionOptions.RemoteUserId = RemoteUserID;
			closeConnectionOptions.SocketId = NetworkManagerSystemEOS.socketId;
			CloseConnectionOptions options = closeConnectionOptions;
			p2PInterface.CloseConnection(ref options);
		}
		base.Dispose(disposing);
	}

	public static EOSNetworkConnection Find(ProductUserId owner, ProductUserId endpoint)
	{
		return instancesList.Where((EOSNetworkConnection instance) => instance.RemoteUserID == endpoint).FirstOrDefault((EOSNetworkConnection instance) => (object)owner == instance.LocalUserID);
	}
}

using System.Collections.Generic;
using Facepunch.Steamworks;
using JetBrains.Annotations;
using RoR2.ConVar;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Networking;

public class SteamNetworkConnection : NetworkConnection
{
	private BaseSteamworks steamworks;

	private Facepunch.Steamworks.Networking networking;

	private static List<SteamNetworkConnection> instancesList = new List<SteamNetworkConnection>();

	public bool ignore;

	public uint rtt;

	public static BoolConVar cvNetP2PDebugTransport = new BoolConVar("net_p2p_debug_transport", ConVarFlags.None, "0", "Allows p2p transport information to print to the console.");

	private static BoolConVar cvNetP2PLogMessages = new BoolConVar("net_p2p_log_messages", ConVarFlags.None, "0", "Enables logging of network messages.");

	public PlatformID myId { get; private set; }

	public PlatformID steamId { get; private set; }

	public SteamNetworkConnection()
	{
	}

	public SteamNetworkConnection([NotNull] BaseSteamworks steamworks, PlatformID endpointId)
	{
		myId = new PlatformID((steamworks as Client)?.SteamId ?? (steamworks as Server)?.SteamId ?? 0);
		steamId = endpointId;
		this.steamworks = steamworks;
		networking = steamworks.Networking;
		networking.CloseSession(endpointId.ID);
		instancesList.Add(this);
	}

	public bool SendConnectionRequest()
	{
		return networking.SendP2PPacket(steamId.ID, null, 0);
	}

	public override bool TransportSend(byte[] bytes, int numBytes, int channelId, out byte error)
	{
		if (ignore)
		{
			error = 0;
			return true;
		}
		logNetworkMessages = cvNetP2PLogMessages.value;
		if (steamId == myId)
		{
			TransportReceive(bytes, numBytes, channelId);
			error = 0;
			if (cvNetP2PDebugTransport.value)
			{
				Debug.LogFormat("SteamNetworkConnection.TransportSend steamId=self numBytes={1} channelId={2}", numBytes, channelId);
			}
			return true;
		}
		Facepunch.Steamworks.Networking.SendType eP2PSendType = Facepunch.Steamworks.Networking.SendType.Reliable;
		QosType qOS = PlatformSystems.networkManager.connectionConfig.Channels[channelId].QOS;
		if (qOS == QosType.Unreliable || qOS == QosType.UnreliableFragmented || qOS == QosType.UnreliableSequenced)
		{
			eP2PSendType = Facepunch.Steamworks.Networking.SendType.Unreliable;
		}
		if (networking.SendP2PPacket(steamId.ID, bytes, numBytes, eP2PSendType))
		{
			error = 0;
			if (cvNetP2PDebugTransport.value)
			{
				Debug.LogFormat("SteamNetworkConnection.TransportSend steamId={0} numBytes={1} channelId={2} error={3}", steamId.value, numBytes, channelId, error);
			}
			return true;
		}
		error = 1;
		if (cvNetP2PDebugTransport.value)
		{
			Debug.LogFormat("SteamNetworkConnection.TransportSend steamId={0} numBytes={1} channelId={2} error={3}", steamId.value, numBytes, channelId, error);
		}
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
		if (networking != null && steamId.ID != 0L)
		{
			networking.CloseSession(steamId.ID);
			steamId = PlatformID.nil;
		}
		base.Dispose(disposing);
	}

	public static SteamNetworkConnection Find(BaseSteamworks owner, PlatformID endpoint)
	{
		for (int i = 0; i < instancesList.Count; i++)
		{
			SteamNetworkConnection steamNetworkConnection = instancesList[i];
			if (steamNetworkConnection.steamId == endpoint && owner == steamNetworkConnection.steamworks)
			{
				return steamNetworkConnection;
			}
		}
		return null;
	}
}

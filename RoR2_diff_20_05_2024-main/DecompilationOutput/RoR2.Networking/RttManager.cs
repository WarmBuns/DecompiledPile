using System;
using HG;
using RoR2.ConVar;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Networking;

public static class RttManager
{
	private struct ConnectionRttInfo
	{
		public readonly NetworkConnection connection;

		public float newestRttInSeconds;

		public uint newestRttInMilliseconds;

		public float frameSmoothedRtt;

		public float frameVelocity;

		public float fixedSmoothedRtt;

		public float fixedVelocity;

		public ConnectionRttInfo(NetworkConnection connection)
		{
			this.connection = connection;
			newestRttInMilliseconds = 0u;
			newestRttInSeconds = 0f;
			frameSmoothedRtt = 0f;
			frameVelocity = 0f;
			fixedSmoothedRtt = 0f;
			fixedVelocity = 0f;
		}
	}

	private class PingMessage : MessageBase
	{
		public uint timeStampMs;

		public override void Serialize(NetworkWriter writer)
		{
			writer.WritePackedUInt32(timeStampMs);
		}

		public override void Deserialize(NetworkReader reader)
		{
			timeStampMs = reader.ReadPackedUInt32();
		}
	}

	private static ConnectionRttInfo[] entries = Array.Empty<ConnectionRttInfo>();

	private static readonly FloatConVar cvRttSmoothDuration = new FloatConVar("net_rtt_smooth_duration", ConVarFlags.None, "0.1", "The smoothing duration for round-trip time values.");

	public static float GetConnectionRTT(NetworkConnection connection)
	{
		if (FindConnectionIndex(connection, out var entryIndex))
		{
			return entries[entryIndex].newestRttInSeconds;
		}
		return 0f;
	}

	public static uint GetConnectionRTTInMilliseconds(NetworkConnection connection)
	{
		if (FindConnectionIndex(connection, out var entryIndex))
		{
			return entries[entryIndex].newestRttInMilliseconds;
		}
		return 0u;
	}

	public static float GetConnectionFrameSmoothedRtt(NetworkConnection connection)
	{
		if (FindConnectionIndex(connection, out var entryIndex))
		{
			return entries[entryIndex].frameSmoothedRtt;
		}
		return 0f;
	}

	public static float GetConnectionFixedSmoothedRtt(NetworkConnection connection)
	{
		if (FindConnectionIndex(connection, out var entryIndex))
		{
			return entries[entryIndex].fixedSmoothedRtt;
		}
		return 0f;
	}

	private static void OnConnectionDiscovered(NetworkConnection connection)
	{
		ArrayUtils.ArrayAppend(ref entries, new ConnectionRttInfo(connection));
	}

	private static void OnConnectionLost(NetworkConnection connection)
	{
		if (FindConnectionIndex(connection, out var entryIndex))
		{
			ArrayUtils.ArrayRemoveAtAndResize(ref entries, entryIndex);
		}
	}

	private static bool FindConnectionIndex(NetworkConnection connection, out int entryIndex)
	{
		ConnectionRttInfo[] array = entries;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].connection == connection)
			{
				entryIndex = i;
				return true;
			}
		}
		entryIndex = -1;
		return false;
	}

	private static void UpdateFilteredRtt(float deltaTime, float targetValue, ref float currentValue, ref float velocity)
	{
		if (currentValue == 0f)
		{
			currentValue = targetValue;
			velocity = 0f;
		}
		else
		{
			currentValue = Mathf.SmoothDamp(currentValue, targetValue, ref velocity, cvRttSmoothDuration.value, 100f, deltaTime);
		}
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		NetworkManagerSystem.onClientConnectGlobal += OnConnectionDiscovered;
		NetworkManagerSystem.onClientDisconnectGlobal += OnConnectionLost;
		NetworkManagerSystem.onServerConnectGlobal += OnConnectionDiscovered;
		NetworkManagerSystem.onServerDisconnectGlobal += OnConnectionLost;
		RoR2Application.onUpdate += Update;
		RoR2Application.onFixedUpdate += FixedUpdate;
	}

	private static void Update()
	{
		float deltaTime = Time.deltaTime;
		ConnectionRttInfo[] array = entries;
		for (int i = 0; i < array.Length; i++)
		{
			ref ConnectionRttInfo reference = ref array[i];
			UpdateFilteredRtt(deltaTime, reference.newestRttInSeconds, ref reference.frameSmoothedRtt, ref reference.frameVelocity);
		}
	}

	private static void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		ConnectionRttInfo[] array = entries;
		for (int i = 0; i < array.Length; i++)
		{
			ref ConnectionRttInfo reference = ref array[i];
			UpdateFilteredRtt(fixedDeltaTime, reference.newestRttInSeconds, ref reference.fixedSmoothedRtt, ref reference.fixedVelocity);
		}
	}

	[NetworkMessageHandler(msgType = 65, client = true, server = true)]
	private static void HandlePing(NetworkMessage netMsg)
	{
		NetworkReader reader = netMsg.reader;
		netMsg.conn.SendByChannel(66, reader.ReadMessage<PingMessage>(), netMsg.channelId);
	}

	[NetworkMessageHandler(msgType = 66, client = true, server = true)]
	private static void HandlePingResponse(NetworkMessage netMsg)
	{
		uint timeStampMs = netMsg.reader.ReadMessage<PingMessage>().timeStampMs;
		uint num = (uint)(int)RoR2Application.stopwatch.ElapsedMilliseconds - timeStampMs;
		if (FindConnectionIndex(netMsg.conn, out var entryIndex))
		{
			ref ConnectionRttInfo reference = ref entries[entryIndex];
			reference.newestRttInMilliseconds = num;
			reference.newestRttInSeconds = (float)num * 0.001f;
		}
	}

	public static void Ping(NetworkConnection conn, int channelId)
	{
		conn.SendByChannel(65, new PingMessage
		{
			timeStampMs = (uint)RoR2Application.stopwatch.ElapsedMilliseconds
		}, channelId);
	}
}

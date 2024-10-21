using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Networking;

public class NetworkedViewAngles : NetworkBehaviour
{
	public PitchYawPair viewAngles;

	private PitchYawPair networkDesiredViewAngles;

	private PitchYawPair velocity;

	private NetworkIdentity networkIdentity;

	public float sendRate = 0.05f;

	public float bufferMultiplier = 3f;

	public float maxSmoothVelocity = 1440f;

	private float sendTimer;

	private static int kCmdCmdUpdateViewAngles;

	public bool hasEffectiveAuthority { get; private set; }

	private void Awake()
	{
		networkIdentity = GetComponent<NetworkIdentity>();
	}

	private void Update()
	{
		hasEffectiveAuthority = Util.HasEffectiveAuthority(networkIdentity);
		if (hasEffectiveAuthority)
		{
			networkDesiredViewAngles = viewAngles;
		}
		else
		{
			viewAngles = PitchYawPair.SmoothDamp(viewAngles, networkDesiredViewAngles, ref velocity, GetNetworkSendInterval() * bufferMultiplier, maxSmoothVelocity);
		}
	}

	public override float GetNetworkSendInterval()
	{
		return sendRate;
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			SetDirtyBit(1u);
		}
		hasEffectiveAuthority = Util.HasEffectiveAuthority(networkIdentity);
		if (!hasEffectiveAuthority)
		{
			return;
		}
		networkDesiredViewAngles = viewAngles;
		if (!NetworkServer.active)
		{
			sendTimer -= Time.deltaTime;
			if (sendTimer <= 0f)
			{
				CallCmdUpdateViewAngles(viewAngles.pitch, viewAngles.yaw);
				sendTimer = GetNetworkSendInterval();
			}
		}
	}

	[Command(channel = 5)]
	public void CmdUpdateViewAngles(float pitch, float yaw)
	{
		networkDesiredViewAngles = new PitchYawPair(pitch, yaw);
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		writer.Write(networkDesiredViewAngles);
		return true;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		PitchYawPair pitchYawPair = reader.ReadPitchYawPair();
		if (!hasEffectiveAuthority)
		{
			networkDesiredViewAngles = pitchYawPair;
			if (initialState)
			{
				viewAngles = pitchYawPair;
				velocity = PitchYawPair.zero;
			}
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdUpdateViewAngles(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdUpdateViewAngles called on client.");
		}
		else
		{
			((NetworkedViewAngles)obj).CmdUpdateViewAngles(reader.ReadSingle(), reader.ReadSingle());
		}
	}

	public void CallCmdUpdateViewAngles(float pitch, float yaw)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdUpdateViewAngles called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdUpdateViewAngles(pitch, yaw);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdUpdateViewAngles);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(pitch);
		networkWriter.Write(yaw);
		SendCommandInternal(networkWriter, 5, "CmdUpdateViewAngles");
	}

	static NetworkedViewAngles()
	{
		kCmdCmdUpdateViewAngles = -1684781536;
		NetworkBehaviour.RegisterCommandDelegate(typeof(NetworkedViewAngles), kCmdCmdUpdateViewAngles, InvokeCmdCmdUpdateViewAngles);
		NetworkCRC.RegisterBehaviour("NetworkedViewAngles", 0);
	}

	public override void PreStartClient()
	{
	}
}

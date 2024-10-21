using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class ShrineBehavior : NetworkBehaviour
{
	private static int kRpcRpcSetPingable;

	[ClientRpc]
	protected virtual void RpcSetPingable(bool value)
	{
		NetworkIdentity component = GetComponent<NetworkIdentity>();
		if ((bool)component)
		{
			component.isPingable = value;
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcSetPingable(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcSetPingable called on server.");
		}
		else
		{
			((ShrineBehavior)obj).RpcSetPingable(reader.ReadBoolean());
		}
	}

	public void CallRpcSetPingable(bool value)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcSetPingable called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcSetPingable);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(value);
		SendRPCInternal(networkWriter, 0, "RpcSetPingable");
	}

	static ShrineBehavior()
	{
		kRpcRpcSetPingable = 85555619;
		NetworkBehaviour.RegisterRpcDelegate(typeof(ShrineBehavior), kRpcRpcSetPingable, InvokeRpcRpcSetPingable);
		NetworkCRC.RegisterBehaviour("ShrineBehavior", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}

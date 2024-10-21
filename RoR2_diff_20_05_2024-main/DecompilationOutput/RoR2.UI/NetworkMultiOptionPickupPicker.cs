using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.UI;

public class NetworkMultiOptionPickupPicker : NetworkBehaviour
{
	[SerializeField]
	private PickupPickerPanel pickupPickerPanel;

	private static int kRpcRpcRemovePickupButtonAvailability;

	private static int kCmdCmdRemovePickupButtonAvailability;

	public void RemovePickupButtonAvailability(int pickupOption)
	{
		if (NetworkServer.active)
		{
			ServerRemovePickupButtonAvailability(pickupOption);
		}
		else
		{
			CallCmdRemovePickupButtonAvailability(pickupOption);
		}
	}

	[Server]
	private void ServerRemovePickupButtonAvailability(int pickupOption)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.UI.NetworkMultiOptionPickupPicker::ServerRemovePickupButtonAvailability(System.Int32)' called on client");
			return;
		}
		pickupPickerPanel.RemovePickupButtonAvailability(pickupOption);
		CallRpcRemovePickupButtonAvailability(pickupOption);
	}

	[ClientRpc]
	private void RpcRemovePickupButtonAvailability(int pickupOption)
	{
		pickupPickerPanel.RemovePickupButtonAvailability(pickupOption);
	}

	[Command]
	private void CmdRemovePickupButtonAvailability(int pickupOption)
	{
		pickupPickerPanel.RemovePickupButtonAvailability(pickupOption);
		CallRpcRemovePickupButtonAvailability(pickupOption);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdRemovePickupButtonAvailability(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdRemovePickupButtonAvailability called on client.");
		}
		else
		{
			((NetworkMultiOptionPickupPicker)obj).CmdRemovePickupButtonAvailability((int)reader.ReadPackedUInt32());
		}
	}

	public void CallCmdRemovePickupButtonAvailability(int pickupOption)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdRemovePickupButtonAvailability called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdRemovePickupButtonAvailability(pickupOption);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdRemovePickupButtonAvailability);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32((uint)pickupOption);
		SendCommandInternal(networkWriter, 0, "CmdRemovePickupButtonAvailability");
	}

	protected static void InvokeRpcRpcRemovePickupButtonAvailability(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcRemovePickupButtonAvailability called on server.");
		}
		else
		{
			((NetworkMultiOptionPickupPicker)obj).RpcRemovePickupButtonAvailability((int)reader.ReadPackedUInt32());
		}
	}

	public void CallRpcRemovePickupButtonAvailability(int pickupOption)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcRemovePickupButtonAvailability called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcRemovePickupButtonAvailability);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32((uint)pickupOption);
		SendRPCInternal(networkWriter, 0, "RpcRemovePickupButtonAvailability");
	}

	static NetworkMultiOptionPickupPicker()
	{
		kCmdCmdRemovePickupButtonAvailability = 752063031;
		NetworkBehaviour.RegisterCommandDelegate(typeof(NetworkMultiOptionPickupPicker), kCmdCmdRemovePickupButtonAvailability, InvokeCmdCmdRemovePickupButtonAvailability);
		kRpcRpcRemovePickupButtonAvailability = 752475021;
		NetworkBehaviour.RegisterRpcDelegate(typeof(NetworkMultiOptionPickupPicker), kRpcRpcRemovePickupButtonAvailability, InvokeRpcRpcRemovePickupButtonAvailability);
		NetworkCRC.RegisterBehaviour("NetworkMultiOptionPickupPicker", 0);
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

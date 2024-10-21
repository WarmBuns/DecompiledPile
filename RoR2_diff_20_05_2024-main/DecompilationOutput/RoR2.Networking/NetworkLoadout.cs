using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Networking;

public class NetworkLoadout : NetworkBehaviour
{
	private static readonly Loadout temp;

	private readonly Loadout loadout = new Loadout();

	private static int kCmdCmdSendLoadout;

	public event Action onLoadoutUpdated;

	public void CopyLoadout(Loadout dest)
	{
		loadout.Copy(dest);
	}

	public void SetLoadout(Loadout src)
	{
		src.Copy(loadout);
		if (NetworkServer.active)
		{
			SetDirtyBit(1u);
		}
		else if (base.isLocalPlayer)
		{
			SendLoadoutClient();
		}
		OnLoadoutUpdated();
	}

	[Command]
	private void CmdSendLoadout(byte[] bytes)
	{
		NetworkReader reader = new NetworkReader(bytes);
		temp.Deserialize(reader);
		SetLoadout(temp);
	}

	[Client]
	private void SendLoadoutClient()
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void RoR2.Networking.NetworkLoadout::SendLoadoutClient()' called on server");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		loadout.Serialize(networkWriter);
		CallCmdSendLoadout(networkWriter.ToArray());
	}

	private void OnLoadoutUpdated()
	{
		this.onLoadoutUpdated?.Invoke();
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		uint num = base.syncVarDirtyBits;
		if (initialState)
		{
			num = 1u;
		}
		writer.WritePackedUInt32(num);
		if ((num & (true ? 1u : 0u)) != 0)
		{
			loadout.Serialize(writer);
		}
		return num != 0;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if ((reader.ReadPackedUInt32() & (true ? 1u : 0u)) != 0)
		{
			temp.Deserialize(reader);
			if (!base.isLocalPlayer)
			{
				temp.Copy(loadout);
				OnLoadoutUpdated();
			}
		}
	}

	static NetworkLoadout()
	{
		temp = new Loadout();
		kCmdCmdSendLoadout = 1217513894;
		NetworkBehaviour.RegisterCommandDelegate(typeof(NetworkLoadout), kCmdCmdSendLoadout, InvokeCmdCmdSendLoadout);
		NetworkCRC.RegisterBehaviour("NetworkLoadout", 0);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdSendLoadout(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSendLoadout called on client.");
		}
		else
		{
			((NetworkLoadout)obj).CmdSendLoadout(reader.ReadBytesAndSize());
		}
	}

	public void CallCmdSendLoadout(byte[] bytes)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSendLoadout called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSendLoadout(bytes);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSendLoadout);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WriteBytesFull(bytes);
		SendCommandInternal(networkWriter, 0, "CmdSendLoadout");
	}

	public override void PreStartClient()
	{
	}
}

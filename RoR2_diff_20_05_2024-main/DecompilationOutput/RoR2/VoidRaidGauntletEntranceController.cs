using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class VoidRaidGauntletEntranceController : NetworkBehaviour
{
	[SerializeField]
	private MapZone entranceZone;

	[SyncVar(hook = "UpdateGauntletIndex")]
	private int gauntletIndex;

	private static int kRpcRpcUpdateGauntletIndex;

	public int NetworkgauntletIndex
	{
		get
		{
			return gauntletIndex;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				UpdateGauntletIndex(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref gauntletIndex, 1u);
		}
	}

	[Server]
	public void SetGauntletIndex(int newGauntletIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.VoidRaidGauntletEntranceController::SetGauntletIndex(System.Int32)' called on client");
			return;
		}
		NetworkgauntletIndex = newGauntletIndex;
		UpdateGauntletIndex(gauntletIndex);
		CallRpcUpdateGauntletIndex(gauntletIndex);
	}

	private void OnEnable()
	{
		if ((bool)entranceZone)
		{
			entranceZone.onBodyTeleport += OnBodyTeleport;
		}
	}

	private void OnDisable()
	{
		if ((bool)entranceZone)
		{
			entranceZone.onBodyTeleport -= OnBodyTeleport;
		}
	}

	private void OnBodyTeleport(CharacterBody body)
	{
		if (Util.HasEffectiveAuthority(body.gameObject) && (bool)body.masterObject.GetComponent<PlayerCharacterMasterController>() && (bool)VoidRaidGauntletController.instance)
		{
			body.healthComponent.CallCmdHealFull();
			body.healthComponent.CallCmdRechargeShieldFull();
			VoidRaidGauntletController.instance.OnAuthorityPlayerEnter();
		}
	}

	private void UpdateGauntletIndex(int newGauntletIndex)
	{
		if (newGauntletIndex != gauntletIndex)
		{
			NetworkgauntletIndex = newGauntletIndex;
		}
		if ((bool)entranceZone && (bool)VoidRaidGauntletController.instance)
		{
			VoidRaidGauntletController.instance.PointZoneToGauntlet(newGauntletIndex, entranceZone);
		}
	}

	[ClientRpc]
	private void RpcUpdateGauntletIndex(int newGauntletIndex)
	{
		UpdateGauntletIndex(newGauntletIndex);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcUpdateGauntletIndex(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcUpdateGauntletIndex called on server.");
		}
		else
		{
			((VoidRaidGauntletEntranceController)obj).RpcUpdateGauntletIndex((int)reader.ReadPackedUInt32());
		}
	}

	public void CallRpcUpdateGauntletIndex(int newGauntletIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcUpdateGauntletIndex called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcUpdateGauntletIndex);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32((uint)newGauntletIndex);
		SendRPCInternal(networkWriter, 0, "RpcUpdateGauntletIndex");
	}

	static VoidRaidGauntletEntranceController()
	{
		kRpcRpcUpdateGauntletIndex = -330092625;
		NetworkBehaviour.RegisterRpcDelegate(typeof(VoidRaidGauntletEntranceController), kRpcRpcUpdateGauntletIndex, InvokeRpcRpcUpdateGauntletIndex);
		NetworkCRC.RegisterBehaviour("VoidRaidGauntletEntranceController", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.WritePackedUInt32((uint)gauntletIndex);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.WritePackedUInt32((uint)gauntletIndex);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			gauntletIndex = (int)reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			UpdateGauntletIndex((int)reader.ReadPackedUInt32());
		}
	}

	public override void PreStartClient()
	{
	}
}

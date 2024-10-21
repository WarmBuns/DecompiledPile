using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class GenericOwnership : NetworkBehaviour
{
	[SyncVar(hook = "SetOwnerClient")]
	private NetworkInstanceId ownerInstanceId;

	private GameObject cachedOwnerObject;

	public GameObject ownerObject
	{
		get
		{
			return cachedOwnerObject;
		}
		[Server]
		set
		{
			if (!NetworkServer.active)
			{
				Debug.LogWarning("[Server] function 'System.Void RoR2.GenericOwnership::set_ownerObject(UnityEngine.GameObject)' called on client");
				return;
			}
			if (!value)
			{
				value = null;
			}
			if ((object)cachedOwnerObject != value)
			{
				cachedOwnerObject = value;
				NetworkownerInstanceId = cachedOwnerObject?.GetComponent<NetworkIdentity>()?.netId ?? NetworkInstanceId.Invalid;
				this.onOwnerChanged?.Invoke(cachedOwnerObject);
			}
		}
	}

	public NetworkInstanceId NetworkownerInstanceId
	{
		get
		{
			return ownerInstanceId;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				SetOwnerClient(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref ownerInstanceId, 1u);
		}
	}

	public event Action<GameObject> onOwnerChanged;

	private void SetOwnerClient(NetworkInstanceId id)
	{
		if (!NetworkServer.active)
		{
			cachedOwnerObject = ClientScene.FindLocalObject(id);
			this.onOwnerChanged?.Invoke(cachedOwnerObject);
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		SetOwnerClient(ownerInstanceId);
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(ownerInstanceId);
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
			writer.Write(ownerInstanceId);
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
			ownerInstanceId = reader.ReadNetworkId();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			SetOwnerClient(reader.ReadNetworkId());
		}
	}

	public override void PreStartClient()
	{
	}
}

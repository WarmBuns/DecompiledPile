using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class ForceSpectate : NetworkBehaviour
{
	[SyncVar]
	public GameObject target;

	private NetworkInstanceId ___targetNetId;

	public GameObject Networktarget
	{
		get
		{
			return target;
		}
		[param: In]
		set
		{
			SetSyncVarGameObject(value, ref target, 1u, ref ___targetNetId);
		}
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
	}

	private void OnDisable()
	{
		InstanceTracker.Remove(this);
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(target);
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
			writer.Write(target);
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
			___targetNetId = reader.ReadNetworkId();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			target = reader.ReadGameObject();
		}
	}

	public override void PreStartClient()
	{
		if (!___targetNetId.IsEmpty())
		{
			Networktarget = ClientScene.FindLocalObject(___targetNetId);
		}
	}
}

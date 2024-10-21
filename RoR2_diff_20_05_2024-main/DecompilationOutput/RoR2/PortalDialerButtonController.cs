using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class PortalDialerButtonController : NetworkBehaviour
{
	[SyncVar(hook = "OnSyncDigitIndex")]
	public byte currentDigitIndex = 1;

	public GameObject holderObject;

	public GameObject swapEffect;

	public ArtifactCompoundDef[] digitDefs;

	private GameObject modelInstance;

	public ArtifactCompoundDef currentDigitDef => digitDefs[currentDigitIndex];

	public byte NetworkcurrentDigitIndex
	{
		get
		{
			return currentDigitIndex;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncDigitIndex(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref currentDigitIndex, 1u);
		}
	}

	private void OnSyncDigitIndex(byte newDigitIndex)
	{
		NetworkcurrentDigitIndex = newDigitIndex;
		if ((bool)modelInstance)
		{
			Object.Destroy(modelInstance);
		}
		if ((bool)swapEffect)
		{
			swapEffect.SetActive(value: false);
			swapEffect.SetActive(value: true);
		}
		if ((bool)currentDigitDef.modelPrefab)
		{
			modelInstance = Object.Instantiate(currentDigitDef.modelPrefab, holderObject.transform);
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		OnSyncDigitIndex(currentDigitIndex);
	}

	[Server]
	public void CycleDigitServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PortalDialerButtonController::CycleDigitServer()' called on client");
			return;
		}
		byte b = (byte)(currentDigitIndex + 1);
		if (b >= digitDefs.Length)
		{
			b = 1;
		}
		NetworkcurrentDigitIndex = b;
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.WritePackedUInt32(currentDigitIndex);
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
			writer.WritePackedUInt32(currentDigitIndex);
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
			currentDigitIndex = (byte)reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			OnSyncDigitIndex((byte)reader.ReadPackedUInt32());
		}
	}

	public override void PreStartClient()
	{
	}
}

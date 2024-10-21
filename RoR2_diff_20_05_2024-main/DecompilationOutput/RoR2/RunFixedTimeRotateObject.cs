using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class RunFixedTimeRotateObject : NetworkBehaviour
{
	private const float ERROR_THRESHOLD = 0.1f;

	[SerializeField]
	private Vector3 eulerVelocity;

	[SerializeField]
	private bool isLocal = true;

	[SyncVar]
	private Quaternion initialRotation = Quaternion.identity;

	private float clientFixedTime;

	private Quaternion desiredRotation = Quaternion.identity;

	private Quaternion currentRotation = Quaternion.identity;

	public Quaternion NetworkinitialRotation
	{
		get
		{
			return initialRotation;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref initialRotation, 1u);
		}
	}

	private void Start()
	{
		if ((bool)Run.instance)
		{
			clientFixedTime = Run.instance.fixedTime;
		}
		if (isLocal)
		{
			NetworkinitialRotation = base.transform.localRotation;
		}
		else
		{
			NetworkinitialRotation = base.transform.rotation;
		}
	}

	private void FixedUpdate()
	{
		if ((bool)Run.instance && NetworkServer.active)
		{
			desiredRotation = initialRotation * Quaternion.Euler(eulerVelocity * Run.instance.fixedTime);
			return;
		}
		clientFixedTime += Time.fixedDeltaTime;
		if (Mathf.Abs(Run.instance.fixedTime - clientFixedTime) > 0.1f)
		{
			clientFixedTime = Mathf.Lerp(clientFixedTime, Run.instance.fixedTime, Time.fixedDeltaTime);
		}
		desiredRotation = initialRotation * Quaternion.Euler(eulerVelocity * clientFixedTime);
	}

	private void Update()
	{
		Quaternion quaternion = Quaternion.Lerp(currentRotation, desiredRotation, Time.deltaTime);
		if (isLocal)
		{
			base.transform.localRotation = quaternion;
		}
		else
		{
			base.transform.rotation = quaternion;
		}
		currentRotation = quaternion;
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(initialRotation);
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
			writer.Write(initialRotation);
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
			initialRotation = reader.ReadQuaternion();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			initialRotation = reader.ReadQuaternion();
		}
	}

	public override void PreStartClient()
	{
	}
}

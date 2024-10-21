using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class ZiplineController : NetworkBehaviour
{
	[SyncVar(hook = "SetPointAPosition")]
	private Vector3 pointAPosition;

	[SyncVar(hook = "SetPointBPosition")]
	private Vector3 pointBPosition;

	[SerializeField]
	private Transform pointATransform;

	[SerializeField]
	private Transform pointBTransform;

	public GameObject ziplineVehiclePrefab;

	private ZiplineVehicle currentZiplineA;

	private ZiplineVehicle currentZiplineB;

	public Vector3 NetworkpointAPosition
	{
		get
		{
			return pointAPosition;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				SetPointAPosition(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref pointAPosition, 1u);
		}
	}

	public Vector3 NetworkpointBPosition
	{
		get
		{
			return pointBPosition;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				SetPointBPosition(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref pointBPosition, 2u);
		}
	}

	public void SetPointAPosition(Vector3 position)
	{
		NetworkpointAPosition = position;
		pointATransform.position = pointAPosition;
		pointATransform.LookAt(pointBTransform);
		pointBTransform.LookAt(pointATransform);
	}

	public void SetPointBPosition(Vector3 position)
	{
		NetworkpointBPosition = position;
		pointBTransform.position = pointBPosition;
		pointATransform.LookAt(pointBTransform);
		pointBTransform.LookAt(pointATransform);
	}

	private void RebuildZiplineVehicle(ref ZiplineVehicle ziplineVehicle, Vector3 startPos, Vector3 endPos)
	{
		if ((bool)ziplineVehicle && ziplineVehicle.vehicleSeat.hasPassenger)
		{
			ziplineVehicle = null;
		}
		if (!ziplineVehicle)
		{
			GameObject gameObject = Object.Instantiate(ziplineVehiclePrefab, startPos, Quaternion.identity);
			ziplineVehicle = gameObject.GetComponent<ZiplineVehicle>();
			ziplineVehicle.NetworkendPoint = endPos;
			NetworkServer.Spawn(gameObject);
		}
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			RebuildZiplineVehicle(ref currentZiplineA, pointAPosition, pointBPosition);
			RebuildZiplineVehicle(ref currentZiplineB, pointBPosition, pointAPosition);
		}
	}

	private void OnDestroy()
	{
		if (NetworkServer.active)
		{
			if ((bool)currentZiplineA)
			{
				Object.Destroy(currentZiplineA.gameObject);
			}
			if ((bool)currentZiplineB)
			{
				Object.Destroy(currentZiplineB.gameObject);
			}
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(pointAPosition);
			writer.Write(pointBPosition);
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
			writer.Write(pointAPosition);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(pointBPosition);
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
			pointAPosition = reader.ReadVector3();
			pointBPosition = reader.ReadVector3();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			SetPointAPosition(reader.ReadVector3());
		}
		if (((uint)num & 2u) != 0)
		{
			SetPointBPosition(reader.ReadVector3());
		}
	}

	public override void PreStartClient()
	{
	}
}

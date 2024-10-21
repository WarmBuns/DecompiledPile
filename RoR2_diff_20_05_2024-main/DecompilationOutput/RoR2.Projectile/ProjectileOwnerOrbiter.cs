using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[DisallowMultipleComponent]
[RequireComponent(typeof(ProjectileController))]
public class ProjectileOwnerOrbiter : NetworkBehaviour
{
	[SerializeField]
	[SyncVar]
	private Vector3 offset;

	[SerializeField]
	[SyncVar]
	private float initialDegreesFromOwnerForward;

	[SerializeField]
	[SyncVar]
	private float degreesPerSecond;

	[SerializeField]
	[SyncVar]
	public float radius;

	[SerializeField]
	[SyncVar]
	private Vector3 planeNormal = Vector3.up;

	private Transform ownerTransform;

	private Rigidbody rigidBody;

	private bool resetOnAcquireOwner = true;

	[SyncVar]
	private Vector3 initialRadialDirection;

	[SyncVar]
	private float initialRunTime;

	public Vector3 Networkoffset
	{
		get
		{
			return offset;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref offset, 1u);
		}
	}

	public float NetworkinitialDegreesFromOwnerForward
	{
		get
		{
			return initialDegreesFromOwnerForward;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref initialDegreesFromOwnerForward, 2u);
		}
	}

	public float NetworkdegreesPerSecond
	{
		get
		{
			return degreesPerSecond;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref degreesPerSecond, 4u);
		}
	}

	public float Networkradius
	{
		get
		{
			return radius;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref radius, 8u);
		}
	}

	public Vector3 NetworkplaneNormal
	{
		get
		{
			return planeNormal;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref planeNormal, 16u);
		}
	}

	public Vector3 NetworkinitialRadialDirection
	{
		get
		{
			return initialRadialDirection;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref initialRadialDirection, 32u);
		}
	}

	public float NetworkinitialRunTime
	{
		get
		{
			return initialRunTime;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref initialRunTime, 64u);
		}
	}

	public void Initialize(Vector3 planeNormal, float radius, float degreesPerSecond, float initialDegreesFromOwnerForward)
	{
		NetworkplaneNormal = planeNormal;
		Networkradius = radius;
		NetworkdegreesPerSecond = degreesPerSecond;
		NetworkinitialDegreesFromOwnerForward = initialDegreesFromOwnerForward;
		ResetState();
	}

	private void OnEnable()
	{
		rigidBody = GetComponent<Rigidbody>();
		ProjectileController component = GetComponent<ProjectileController>();
		if ((bool)component.owner)
		{
			AcquireOwner(component);
		}
		else
		{
			component.onInitialized += AcquireOwner;
		}
	}

	public void FixedUpdate()
	{
		UpdatePosition(doSnap: false);
	}

	private void ResetState()
	{
		NetworkinitialRunTime = Time.fixedTime;
		planeNormal.Normalize();
		if ((bool)ownerTransform)
		{
			NetworkinitialRadialDirection = Quaternion.AngleAxis(initialDegreesFromOwnerForward, planeNormal) * ownerTransform.forward;
			resetOnAcquireOwner = false;
		}
		UpdatePosition(doSnap: true);
	}

	private void UpdatePosition(bool doSnap)
	{
		if ((bool)ownerTransform)
		{
			float angle = (Time.fixedTime - initialRunTime) * degreesPerSecond;
			Vector3 position = ownerTransform.position + offset + Quaternion.AngleAxis(angle, planeNormal) * initialRadialDirection * radius;
			if (!rigidBody || doSnap)
			{
				base.transform.position = position;
			}
			else if ((bool)rigidBody)
			{
				rigidBody.MovePosition(position);
			}
		}
	}

	public void SetInitialDegreesFromOwnerForward(float degrees)
	{
		NetworkinitialDegreesFromOwnerForward = degrees;
		if ((bool)ownerTransform)
		{
			NetworkinitialRadialDirection = Quaternion.AngleAxis(initialDegreesFromOwnerForward, planeNormal) * ownerTransform.forward;
		}
	}

	public float GetInitialRunTime()
	{
		return initialRunTime;
	}

	public void SetInitialRunTime(float _time)
	{
		NetworkinitialRunTime = Mathf.Max(_time, 0f);
	}

	private void AcquireOwner(ProjectileController controller)
	{
		ownerTransform = controller.owner.transform;
		controller.onInitialized -= AcquireOwner;
		if (resetOnAcquireOwner)
		{
			resetOnAcquireOwner = false;
			ResetState();
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(offset);
			writer.Write(initialDegreesFromOwnerForward);
			writer.Write(degreesPerSecond);
			writer.Write(radius);
			writer.Write(planeNormal);
			writer.Write(initialRadialDirection);
			writer.Write(initialRunTime);
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
			writer.Write(offset);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(initialDegreesFromOwnerForward);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(degreesPerSecond);
		}
		if ((base.syncVarDirtyBits & 8u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(radius);
		}
		if ((base.syncVarDirtyBits & 0x10u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(planeNormal);
		}
		if ((base.syncVarDirtyBits & 0x20u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(initialRadialDirection);
		}
		if ((base.syncVarDirtyBits & 0x40u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(initialRunTime);
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
			offset = reader.ReadVector3();
			initialDegreesFromOwnerForward = reader.ReadSingle();
			degreesPerSecond = reader.ReadSingle();
			radius = reader.ReadSingle();
			planeNormal = reader.ReadVector3();
			initialRadialDirection = reader.ReadVector3();
			initialRunTime = reader.ReadSingle();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			offset = reader.ReadVector3();
		}
		if (((uint)num & 2u) != 0)
		{
			initialDegreesFromOwnerForward = reader.ReadSingle();
		}
		if (((uint)num & 4u) != 0)
		{
			degreesPerSecond = reader.ReadSingle();
		}
		if (((uint)num & 8u) != 0)
		{
			radius = reader.ReadSingle();
		}
		if (((uint)num & 0x10u) != 0)
		{
			planeNormal = reader.ReadVector3();
		}
		if (((uint)num & 0x20u) != 0)
		{
			initialRadialDirection = reader.ReadVector3();
		}
		if (((uint)num & 0x40u) != 0)
		{
			initialRunTime = reader.ReadSingle();
		}
	}

	public override void PreStartClient()
	{
	}
}

using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(VehicleSeat))]
public class ZiplineVehicle : NetworkBehaviour
{
	public float maxSpeed = 30f;

	public float acceleration = 2f;

	private Rigidbody rigidbody;

	private Vector3 startPoint;

	[SyncVar]
	public Vector3 endPoint;

	private Vector3 travelDirection;

	private GameObject currentPassenger;

	private Run.FixedTimeStamp startTravelFixedTime = Run.FixedTimeStamp.positiveInfinity;

	private Run.TimeStamp startTravelTime = Run.TimeStamp.positiveInfinity;

	public VehicleSeat vehicleSeat { get; private set; }

	public Vector3 NetworkendPoint
	{
		get
		{
			return endPoint;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref endPoint, 1u);
		}
	}

	private void Awake()
	{
		vehicleSeat = GetComponent<VehicleSeat>();
		rigidbody = GetComponent<Rigidbody>();
		vehicleSeat.onPassengerEnter += OnPassengerEnter;
		vehicleSeat.onPassengerExit += OnPassengerExit;
	}

	private void OnPassengerEnter(GameObject passenger)
	{
		currentPassenger = passenger;
		startPoint = base.transform.position;
		startTravelFixedTime = Run.FixedTimeStamp.now;
		startTravelTime = Run.TimeStamp.now;
	}

	private void SetTravelDistance(float time)
	{
		Vector3 vector = endPoint - startPoint;
		float magnitude = vector.magnitude;
		Vector3 vector2 = vector / magnitude;
		float num = HGPhysics.CalculateDistance(0f, acceleration, time);
		bool flag = false;
		if (num > magnitude)
		{
			num = magnitude;
			flag = true;
		}
		rigidbody.MovePosition(startPoint + vector2 * num);
		rigidbody.velocity = vector2 * (acceleration * time);
		if (NetworkServer.active && flag)
		{
			vehicleSeat.EjectPassenger(currentPassenger);
		}
	}

	private void Update()
	{
		_ = startTravelTime.hasPassed;
	}

	private void FixedUpdate()
	{
		if (startTravelFixedTime.hasPassed)
		{
			SetTravelDistance(startTravelFixedTime.timeSince);
		}
		if (!NetworkServer.active || !currentPassenger)
		{
			return;
		}
		Vector3 normalized = (endPoint - base.transform.position).normalized;
		if (Vector3.Dot(normalized, travelDirection) < 0f)
		{
			vehicleSeat.EjectPassenger(currentPassenger);
			return;
		}
		float fixedDeltaTime = Time.fixedDeltaTime;
		Vector3 velocity = rigidbody.velocity;
		velocity += travelDirection * (acceleration * fixedDeltaTime);
		float sqrMagnitude = velocity.sqrMagnitude;
		if (sqrMagnitude > maxSpeed * maxSpeed)
		{
			float num = Mathf.Sqrt(sqrMagnitude);
			velocity *= maxSpeed / num;
		}
		rigidbody.velocity = velocity;
		travelDirection = normalized;
	}

	private void OnPassengerExit(GameObject passenger)
	{
		currentPassenger = null;
		vehicleSeat.enabled = false;
		if (NetworkServer.active)
		{
			base.gameObject.AddComponent<DestroyOnTimer>().duration = 0.1f;
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(endPoint);
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
			writer.Write(endPoint);
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
			endPoint = reader.ReadVector3();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			endPoint = reader.ReadVector3();
		}
	}

	public override void PreStartClient()
	{
	}
}

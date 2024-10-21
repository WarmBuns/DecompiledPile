using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(DelayBlast))]
public class SpiteBombController : NetworkBehaviour
{
	public float radius;

	public float bounce = 0.8f;

	public float minimumBounceVelocity;

	public GameObject[] meshVisuals;

	public string[] bounceSoundStrings;

	private new Transform transform;

	private Rigidbody rb;

	[SyncVar]
	private Vector3 _bouncePosition;

	[SyncVar]
	private float _initialVelocityY;

	private static readonly int maxBounces = 2;

	private Vector3 startPosition;

	private Vector3 velocity;

	private int bounces;

	public DelayBlast delayBlast { get; private set; }

	public Vector3 bouncePosition
	{
		get
		{
			return _bouncePosition;
		}
		set
		{
			Network_bouncePosition = value;
		}
	}

	public float initialVelocityY
	{
		get
		{
			return _initialVelocityY;
		}
		set
		{
			Network_initialVelocityY = value;
		}
	}

	public Vector3 Network_bouncePosition
	{
		get
		{
			return _bouncePosition;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _bouncePosition, 1u);
		}
	}

	public float Network_initialVelocityY
	{
		get
		{
			return _initialVelocityY;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref _initialVelocityY, 2u);
		}
	}

	private void Awake()
	{
		transform = base.transform;
		rb = GetComponent<Rigidbody>();
		delayBlast = GetComponent<DelayBlast>();
	}

	private void Start()
	{
		startPosition = transform.position;
		float time = Trajectory.CalculateFlightDuration(startPosition.y, bouncePosition.y, initialVelocityY);
		Vector3 vector = bouncePosition - startPosition;
		vector.y = 0f;
		float magnitude = vector.magnitude;
		float num = Trajectory.CalculateGroundSpeed(time, magnitude);
		velocity = vector / magnitude * num;
		velocity.y = initialVelocityY;
	}

	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		velocity.y += fixedDeltaTime * Physics.gravity.y;
		Vector3 position = transform.position;
		position += velocity * fixedDeltaTime;
		if (position.y < bouncePosition.y + radius)
		{
			velocity.y = Mathf.Max(velocity.y * (0f - bounce), minimumBounceVelocity);
			velocity.x = 0f;
			velocity.z = 0f;
			position.y = bouncePosition.y + radius;
			OnBounce();
		}
		rb.MovePosition(position);
	}

	private void OnBounce()
	{
		meshVisuals[bounces].SetActive(value: false);
		Util.PlaySound(bounceSoundStrings[bounces], base.gameObject);
		bounces++;
		if (bounces > maxBounces)
		{
			OnFinalBounce();
		}
		else
		{
			meshVisuals[bounces].SetActive(value: true);
		}
	}

	private void OnFinalBounce()
	{
		if (NetworkServer.active)
		{
			delayBlast.position = transform.position;
			delayBlast.Detonate();
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(_bouncePosition);
			writer.Write(_initialVelocityY);
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
			writer.Write(_bouncePosition);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(_initialVelocityY);
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
			_bouncePosition = reader.ReadVector3();
			_initialVelocityY = reader.ReadSingle();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			_bouncePosition = reader.ReadVector3();
		}
		if (((uint)num & 2u) != 0)
		{
			_initialVelocityY = reader.ReadSingle();
		}
	}

	public override void PreStartClient()
	{
	}
}

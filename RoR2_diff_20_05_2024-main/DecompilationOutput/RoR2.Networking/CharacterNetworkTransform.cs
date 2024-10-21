using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Networking;

public class CharacterNetworkTransform : NetworkBehaviour
{
	public struct Snapshot
	{
		public float serverTime;

		public Vector3 position;

		public Vector3 moveVector;

		public Vector3 aimDirection;

		public Quaternion rotation;

		public bool isGrounded;

		public Vector3 groundNormal;

		private static bool LerpGroundNormal(ref Snapshot a, ref Snapshot b, float t, out Vector3 groundNormal)
		{
			groundNormal = Vector3.zero;
			bool num = ((t > 0f) ? b.isGrounded : a.isGrounded);
			if (num)
			{
				if (b.isGrounded)
				{
					if (a.isGrounded)
					{
						groundNormal = Vector3.Slerp(a.groundNormal, b.groundNormal, t);
						return num;
					}
					groundNormal = b.groundNormal;
					return num;
				}
				groundNormal = a.groundNormal;
			}
			return num;
		}

		public static Snapshot Lerp(Snapshot a, Snapshot b, float t)
		{
			Vector3 vector;
			bool flag = LerpGroundNormal(ref a, ref b, t, out vector);
			Snapshot result = default(Snapshot);
			result.position = Vector3.Lerp(a.position, b.position, t);
			result.moveVector = Vector3.Lerp(a.moveVector, b.moveVector, t);
			result.aimDirection = Vector3.Slerp(a.aimDirection, b.moveVector, t);
			result.rotation = Quaternion.Lerp(a.rotation, b.rotation, t);
			result.isGrounded = flag;
			result.groundNormal = vector;
			return result;
		}

		public static Snapshot Interpolate(Snapshot a, Snapshot b, float serverTime)
		{
			float t = (serverTime - a.serverTime) / (b.serverTime - a.serverTime);
			Vector3 vector;
			bool flag = LerpGroundNormal(ref a, ref b, t, out vector);
			Snapshot result = default(Snapshot);
			result.serverTime = serverTime;
			result.position = Vector3.LerpUnclamped(a.position, b.position, t);
			result.moveVector = Vector3.Lerp(a.moveVector, b.moveVector, t);
			result.aimDirection = Vector3.Slerp(a.aimDirection, b.aimDirection, t);
			result.rotation = Quaternion.Lerp(a.rotation, b.rotation, t);
			result.isGrounded = flag;
			result.groundNormal = vector;
			return result;
		}
	}

	private static List<CharacterNetworkTransform> instancesList = new List<CharacterNetworkTransform>();

	private static ReadOnlyCollection<CharacterNetworkTransform> _readOnlyInstancesList = new ReadOnlyCollection<CharacterNetworkTransform>(instancesList);

	[Tooltip("The delay in seconds between position network updates.")]
	public float positionTransmitInterval = 0.1f;

	[HideInInspector]
	public float lastPositionTransmitTime = float.NegativeInfinity;

	[Tooltip("The number of packets of buffers to have.")]
	public float interpolationFactor = 2f;

	public Snapshot newestNetSnapshot;

	private List<Snapshot> snapshots = new List<Snapshot>();

	public bool debugDuplicatePositions;

	public bool debugSnapshotReceived;

	private bool rigidbodyStartedKinematic = true;

	public static ReadOnlyCollection<CharacterNetworkTransform> readOnlyInstancesList => _readOnlyInstancesList;

	public new Transform transform { get; private set; }

	public InputBankTest inputBank { get; private set; }

	public CharacterMotor characterMotor { get; set; }

	public CharacterDirection characterDirection { get; private set; }

	private Rigidbody rigidbody { get; set; }

	public float interpolationDelay => positionTransmitInterval * interpolationFactor;

	public bool hasEffectiveAuthority { get; private set; }

	private Snapshot CalcCurrentSnapshot(float time, float interpolationDelay)
	{
		float num = time - interpolationDelay;
		if (snapshots.Count < 2)
		{
			Snapshot result = ((snapshots.Count == 0) ? BuildSnapshot() : snapshots[0]);
			result.serverTime = num;
			return result;
		}
		int i;
		for (i = 0; i < snapshots.Count - 2 && (!(snapshots[i].serverTime <= num) || !(snapshots[i + 1].serverTime >= num)); i++)
		{
		}
		return Snapshot.Interpolate(snapshots[i], snapshots[i + 1], num);
	}

	private Snapshot BuildSnapshot()
	{
		Snapshot result = default(Snapshot);
		result.serverTime = PlatformSystems.networkManager.serverFixedTime;
		result.position = transform.position;
		result.moveVector = (inputBank ? inputBank.moveVector : Vector3.zero);
		result.aimDirection = (inputBank ? inputBank.aimDirection : Vector3.zero);
		result.rotation = (characterDirection ? Quaternion.Euler(0f, characterDirection.yaw, 0f) : transform.rotation);
		result.isGrounded = (bool)characterMotor && characterMotor.isGrounded;
		result.groundNormal = (characterMotor ? characterMotor.estimatedGroundNormal : Vector3.zero);
		return result;
	}

	public void PushSnapshot(Snapshot newSnapshot)
	{
		if (debugSnapshotReceived)
		{
			Debug.LogFormat("{0} CharacterNetworkTransform snapshot received.", base.gameObject);
		}
		if (snapshots.Count > 0)
		{
			_ = newSnapshot.serverTime;
			_ = snapshots[snapshots.Count - 1].serverTime;
		}
		if (debugDuplicatePositions && snapshots.Count > 0)
		{
			_ = newSnapshot.position == snapshots[snapshots.Count - 1].position;
		}
		if (((snapshots.Count > 0) ? snapshots[snapshots.Count - 1].serverTime : float.NegativeInfinity) < newSnapshot.serverTime)
		{
			snapshots.Add(newSnapshot);
			newestNetSnapshot = newSnapshot;
			Debug.DrawLine(newSnapshot.position + Vector3.up, newSnapshot.position + Vector3.down, Color.white, 0.25f);
		}
		float num = PlatformSystems.networkManager.serverFixedTime - interpolationDelay * 2f;
		while (snapshots.Count > 2 && snapshots[1].serverTime < num)
		{
			snapshots.RemoveAt(0);
		}
	}

	private void Awake()
	{
		transform = base.transform;
		inputBank = GetComponent<InputBankTest>();
		characterMotor = GetComponent<CharacterMotor>();
		characterDirection = GetComponent<CharacterDirection>();
		rigidbody = GetComponent<Rigidbody>();
		if ((bool)rigidbody)
		{
			rigidbodyStartedKinematic = rigidbody.isKinematic;
		}
	}

	private void Start()
	{
		newestNetSnapshot = BuildSnapshot();
		UpdateAuthority();
	}

	private void OnEnable()
	{
		bool num = instancesList.Contains(this);
		instancesList.Add(this);
		if (num)
		{
			Debug.LogError("Instance already in list!");
		}
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
		if (instancesList.Contains(this))
		{
			Debug.LogError("Instance was not fully removed from list!");
		}
	}

	private void UpdateAuthority()
	{
		hasEffectiveAuthority = Util.HasEffectiveAuthority(base.gameObject);
		if ((bool)rigidbody)
		{
			rigidbody.isKinematic = !hasEffectiveAuthority || rigidbodyStartedKinematic;
		}
	}

	public override void OnStartAuthority()
	{
		UpdateAuthority();
	}

	public override void OnStopAuthority()
	{
		UpdateAuthority();
	}

	private void ApplyCurrentSnapshot(float currentTime)
	{
		Snapshot snapshot = CalcCurrentSnapshot(currentTime, interpolationDelay);
		if (!characterMotor)
		{
			if (rigidbodyStartedKinematic)
			{
				transform.position = snapshot.position;
			}
			else
			{
				rigidbody.MovePosition(snapshot.position);
			}
		}
		if ((bool)inputBank)
		{
			inputBank.moveVector = snapshot.moveVector;
			inputBank.aimDirection = snapshot.aimDirection;
		}
		if ((bool)characterMotor)
		{
			characterMotor.netIsGrounded = snapshot.isGrounded;
			characterMotor.netGroundNormal = snapshot.groundNormal;
			if (characterMotor.Motor.enabled)
			{
				characterMotor.Motor.MoveCharacter(snapshot.position);
			}
			else
			{
				characterMotor.Motor.SetPosition(snapshot.position);
			}
		}
		if ((bool)characterDirection)
		{
			characterDirection.yaw = snapshot.rotation.eulerAngles.y;
		}
		else if (rigidbodyStartedKinematic)
		{
			transform.rotation = snapshot.rotation;
		}
		else
		{
			rigidbody.MoveRotation(snapshot.rotation);
		}
	}

	private void FixedUpdate()
	{
		if (!hasEffectiveAuthority)
		{
			ApplyCurrentSnapshot(PlatformSystems.networkManager.serverFixedTime);
		}
		else
		{
			newestNetSnapshot = BuildSnapshot();
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}

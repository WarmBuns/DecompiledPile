using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

public class ProjectileNetworkTransform : NetworkBehaviour
{
	private ProjectileController projectileController;

	private new Transform transform;

	private Rigidbody rb;

	private InterpolatedTransform interpolatedTransform;

	[Tooltip("The delay in seconds between position network updates.")]
	public float positionTransmitInterval = 1f / 30f;

	[Tooltip("The number of packets of buffers to have.")]
	public float interpolationFactor = 1f;

	public bool allowClientsideCollision;

	public bool checkForLocalPlayerAuthority;

	public int singletonArrayIndex = -1;

	private bool hasEffectiveAuthority;

	[SyncVar(hook = "OnSyncPosition")]
	public Vector3 serverPosition;

	[SyncVar(hook = "OnSyncRotation")]
	public Quaternion serverRotation;

	private NetworkLerpedVector3 interpolatedPosition;

	private NetworkLerpedQuaternion interpolatedRotation;

	private bool needsUpdate = true;

	private static int kCmdCmdSetPositionAndRotationFromClient;

	private bool isPrediction
	{
		get
		{
			if (!projectileController)
			{
				return false;
			}
			return projectileController.isPrediction;
		}
	}

	public Vector3 NetworkserverPosition
	{
		get
		{
			return serverPosition;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncPosition(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref serverPosition, 1u);
		}
	}

	public Quaternion NetworkserverRotation
	{
		get
		{
			return serverRotation;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				OnSyncRotation(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref serverRotation, 2u);
		}
	}

	public void SetValuesFromTransform()
	{
		NetworkserverPosition = transform.position;
		NetworkserverRotation = transform.rotation;
		if (singletonArrayIndex != -1)
		{
			Debug.LogError("SetValuesFromTransform called without being registered!");
		}
		needsUpdate = true;
	}

	private void Awake()
	{
		projectileController = GetComponent<ProjectileController>();
		interpolatedTransform = GetComponent<InterpolatedTransform>();
		transform = base.transform;
		NetworkserverPosition = transform.position;
		NetworkserverRotation = transform.rotation;
		rb = GetComponent<Rigidbody>();
	}

	private void Start()
	{
		interpolatedPosition.interpDelay = GetNetworkSendInterval() * interpolationFactor;
		interpolatedPosition.SetValueImmediate(serverPosition);
		interpolatedRotation.SetValueImmediate(serverRotation);
		if (isPrediction)
		{
			base.enabled = false;
		}
		CheckAuthority();
	}

	private void CheckAuthority()
	{
		if (checkForLocalPlayerAuthority)
		{
			hasEffectiveAuthority = Util.HasEffectiveAuthority(base.gameObject);
		}
		else
		{
			hasEffectiveAuthority = NetworkServer.active;
		}
		if ((bool)rb && !isPrediction && !hasEffectiveAuthority)
		{
			rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
			rb.detectCollisions = allowClientsideCollision;
			rb.isKinematic = true;
		}
		base.OnStartAuthority();
	}

	private void OnSyncPosition(Vector3 newPosition)
	{
		interpolatedPosition.PushValue(newPosition);
		NetworkserverPosition = newPosition;
	}

	[Command]
	private void CmdSetPositionAndRotationFromClient(Vector3 position, Quaternion rotation)
	{
		NetworkserverPosition = position;
		NetworkserverRotation = rotation;
	}

	private void OnSyncRotation(Quaternion newRotation)
	{
		interpolatedRotation.PushValue(newRotation);
		NetworkserverRotation = newRotation;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override float GetNetworkSendInterval()
	{
		return positionTransmitInterval;
	}

	private void FixedUpdate()
	{
		MyFixedUpdate(Time.deltaTime);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool NeedsUpdate()
	{
		if (needsUpdate)
		{
			return true;
		}
		if (!(serverPosition != transform.position))
		{
			return serverRotation != transform.rotation;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CheckForUpdateAndPushUpdatesIfNeeded()
	{
		if (NeedsUpdate())
		{
			interpolatedPosition.interpDelay = GetNetworkSendInterval() * interpolationFactor;
			Transform obj = transform;
			Vector3 position = obj.position;
			Quaternion rotation = obj.rotation;
			NetworkserverPosition = position;
			NetworkserverRotation = rotation;
			interpolatedPosition.SetValueImmediate(position);
			interpolatedRotation.SetValueImmediate(rotation);
			needsUpdate = false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CheckForUpdateAndPushUpdatesIfNeededWithCommand()
	{
		if (NeedsUpdate())
		{
			interpolatedPosition.interpDelay = GetNetworkSendInterval() * interpolationFactor;
			Transform obj = transform;
			Vector3 position = obj.position;
			Quaternion rotation = obj.rotation;
			CallCmdSetPositionAndRotationFromClient(position, rotation);
			interpolatedPosition.SetValueImmediate(position);
			interpolatedRotation.SetValueImmediate(rotation);
			needsUpdate = false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Interpolate()
	{
		Vector3 currentValue = interpolatedPosition.GetCurrentValue(hasAuthority: false);
		Quaternion currentValue2 = interpolatedRotation.GetCurrentValue(hasAuthority: false);
		ApplyPositionAndRotation(currentValue, currentValue2);
	}

	private void MyFixedUpdate(float deltaTime)
	{
		if (checkForLocalPlayerAuthority)
		{
			if (hasEffectiveAuthority)
			{
				CheckForUpdateAndPushUpdatesIfNeededWithCommand();
			}
			else
			{
				Interpolate();
			}
		}
		else if (base.isServer)
		{
			CheckForUpdateAndPushUpdatesIfNeeded();
		}
		else
		{
			Interpolate();
		}
	}

	private void ApplyPositionAndRotation(Vector3 position, Quaternion rotation)
	{
		if ((bool)rb && !interpolatedTransform)
		{
			rb.MovePosition(position);
			rb.MoveRotation(rotation);
		}
		else
		{
			transform.position = position;
			transform.rotation = rotation;
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdSetPositionAndRotationFromClient(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdSetPositionAndRotationFromClient called on client.");
		}
		else
		{
			((ProjectileNetworkTransform)obj).CmdSetPositionAndRotationFromClient(reader.ReadVector3(), reader.ReadQuaternion());
		}
	}

	public void CallCmdSetPositionAndRotationFromClient(Vector3 position, Quaternion rotation)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdSetPositionAndRotationFromClient called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdSetPositionAndRotationFromClient(position, rotation);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdSetPositionAndRotationFromClient);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.Write(position);
		networkWriter.Write(rotation);
		SendCommandInternal(networkWriter, 0, "CmdSetPositionAndRotationFromClient");
	}

	static ProjectileNetworkTransform()
	{
		kCmdCmdSetPositionAndRotationFromClient = -895512346;
		NetworkBehaviour.RegisterCommandDelegate(typeof(ProjectileNetworkTransform), kCmdCmdSetPositionAndRotationFromClient, InvokeCmdCmdSetPositionAndRotationFromClient);
		NetworkCRC.RegisterBehaviour("ProjectileNetworkTransform", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(serverPosition);
			writer.Write(serverRotation);
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
			writer.Write(serverPosition);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(serverRotation);
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
			serverPosition = reader.ReadVector3();
			serverRotation = reader.ReadQuaternion();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			OnSyncPosition(reader.ReadVector3());
		}
		if (((uint)num & 2u) != 0)
		{
			OnSyncRotation(reader.ReadQuaternion());
		}
	}

	public override void PreStartClient()
	{
	}
}

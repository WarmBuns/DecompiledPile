using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using KinematicCharacterController;
using RoR2.ConVar;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
public class CharacterMotor : BaseCharacterController, IPhysMotor, ILifeBehavior, IDisplacementReceiver, ICharacterGravityParameterProvider, ICharacterFlightParameterProvider
{
	[Serializable]
	public struct HitGroundInfo
	{
		public Vector3 velocity;

		public Vector3 position;

		public bool isValidForEffect;

		public override string ToString()
		{
			return $"velocity={velocity} position={position}";
		}
	}

	public delegate void HitGroundDelegate(ref HitGroundInfo hitGroundInfo);

	public struct MovementHitInfo
	{
		public Vector3 velocity;

		public Collider hitCollider;
	}

	public delegate void MovementHitDelegate(ref MovementHitInfo movementHitInfo);

	[HideInInspector]
	public float walkSpeedPenaltyCoefficient = 1f;

	[Tooltip("The character direction component to supply a move vector to.")]
	public CharacterDirection characterDirection;

	[Tooltip("Whether or not a move vector supplied to this component can cause movement. Use this when the object is driven by root motion.")]
	public bool muteWalkMotion;

	[Tooltip("The mass of this character.")]
	public float mass = 1f;

	[Tooltip("The air control value of this character as a fraction of ground control.")]
	public float airControl = 0.25f;

	[Tooltip("Disables Air Control for things like jumppads")]
	public bool disableAirControlUntilCollision;

	[Tooltip("Auto-assigns parameters skin width, slope angle, and step offset as a function of the Character Motor's radius and height")]
	public bool generateParametersOnAwake = true;

	[Tooltip("Ignores JumpVolumes")]
	public bool doNotTriggerJumpVolumes;

	private NetworkIdentity networkIdentity;

	private CharacterBody body;

	private CapsuleCollider capsuleCollider;

	private static readonly bool enableMotorWithoutAuthority;

	private bool alive = true;

	private const float restDuration = 1f;

	private const float restVelocityThreshold = 0.025f;

	private const float restVelocityThresholdSqr = 0.00062500004f;

	public const float slipStartAngle = 70f;

	public const float slipEndAngle = 55f;

	private float restStopwatch;

	private Vector3 previousPosition;

	private bool isAirControlForced;

	[NonSerialized]
	public int jumpCount;

	[NonSerialized]
	public bool netIsGrounded;

	[NonSerialized]
	public Vector3 netGroundNormal;

	[NonSerialized]
	public Vector3 velocity;

	private Vector3 lastVelocity;

	[NonSerialized]
	public Vector3 rootMotion;

	private Vector3 _moveDirection;

	private static readonly FloatConVar cvCMotorSafeCollisionStepThreshold;

	private int _safeCollisionEnableCount;

	[SerializeField]
	[Tooltip("Determins how gravity affects this character.")]
	private CharacterGravityParameters _gravityParameters;

	[SerializeField]
	[Tooltip("Determines whether this character has three-dimensional or two-dimensional movement capabilities.")]
	private CharacterFlightParameters _flightParameters;

	private static int kRpcRpcApplyForceImpulse;

	private static int kRpcRpcOnHitGroundSFX;

	private static int kCmdCmdReportHitGround;

	public float walkSpeed => body.moveSpeed * walkSpeedPenaltyCoefficient;

	public float acceleration => body.acceleration;

	public bool atRest => restStopwatch > 1f;

	public bool hasEffectiveAuthority { get; private set; }

	public Vector3 estimatedGroundNormal
	{
		get
		{
			if (!hasEffectiveAuthority)
			{
				return netGroundNormal;
			}
			return base.Motor.GroundingStatus.GroundNormal;
		}
	}

	private bool canWalk
	{
		get
		{
			if (!muteWalkMotion)
			{
				return alive;
			}
			return false;
		}
	}

	public bool isGrounded
	{
		get
		{
			if (!hasEffectiveAuthority)
			{
				return netIsGrounded;
			}
			return base.Motor.GroundingStatus.IsStableOnGround;
		}
	}

	float IPhysMotor.mass => mass;

	Vector3 IPhysMotor.velocity => velocity;

	Vector3 IPhysMotor.velocityAuthority
	{
		get
		{
			return velocity;
		}
		set
		{
			velocity = value;
		}
	}

	public Vector3 moveDirection
	{
		get
		{
			return _moveDirection;
		}
		set
		{
			_moveDirection = value;
		}
	}

	private float slopeLimit
	{
		get
		{
			return base.Motor.MaxStableSlopeAngle;
		}
		set
		{
			base.Motor.MaxStableSlopeAngle = value;
		}
	}

	public float stepOffset
	{
		get
		{
			return base.Motor.MaxStepHeight;
		}
		set
		{
			base.Motor.MaxStepHeight = value;
		}
	}

	public float capsuleHeight => capsuleCollider.height;

	public float capsuleRadius => capsuleCollider.radius;

	public StepHandlingMethod stepHandlingMethod
	{
		get
		{
			return base.Motor.StepHandling = StepHandlingMethod.None;
		}
		set
		{
			base.Motor.StepHandling = value;
		}
	}

	public bool ledgeHandling
	{
		get
		{
			return base.Motor.LedgeAndDenivelationHandling;
		}
		set
		{
			base.Motor.LedgeAndDenivelationHandling = value;
		}
	}

	public bool interactiveRigidbodyHandling
	{
		get
		{
			return base.Motor.InteractiveRigidbodyHandling;
		}
		set
		{
			base.Motor.InteractiveRigidbodyHandling = value;
		}
	}

	public Run.FixedTimeStamp lastGroundedTime { get; private set; } = Run.FixedTimeStamp.negativeInfinity;

	public CharacterGravityParameters gravityParameters
	{
		get
		{
			return _gravityParameters;
		}
		set
		{
			if (!_gravityParameters.Equals(value))
			{
				_gravityParameters = value;
				useGravity = _gravityParameters.CheckShouldUseGravity();
			}
		}
	}

	public bool useGravity { get; private set; }

	public CharacterFlightParameters flightParameters
	{
		get
		{
			return _flightParameters;
		}
		set
		{
			if (!_flightParameters.Equals(value))
			{
				_flightParameters = value;
				isFlying = _flightParameters.CheckShouldUseFlight();
			}
		}
	}

	public bool isFlying { get; private set; }

	[Obsolete("Use '.onHitGroundAuthority' instead, which this is just a backwards-compatibility wrapper for. Or, use '.onHitGroundAuthority' if that is more appropriate to your use case.", false)]
	public event HitGroundDelegate onHitGround
	{
		add
		{
			onHitGroundAuthority += value;
		}
		remove
		{
			onHitGroundAuthority -= value;
		}
	}

	public event HitGroundDelegate onHitGroundAuthority;

	public event Action<CharacterBody> onMotorStart;

	public event MovementHitDelegate onMovementHit;

	private void UpdateInnerMotorEnabled()
	{
		base.Motor.enabled = enableMotorWithoutAuthority || hasEffectiveAuthority;
	}

	private void UpdateAuthority()
	{
		hasEffectiveAuthority = Util.HasEffectiveAuthority(base.gameObject);
		UpdateInnerMotorEnabled();
	}

	private void Awake()
	{
		networkIdentity = GetComponent<NetworkIdentity>();
		body = GetComponent<CharacterBody>();
		capsuleCollider = GetComponent<CapsuleCollider>();
	}

	private void Start()
	{
		previousPosition = base.transform.position;
		if (base.Motor == null)
		{
			SetupCharacterMotor(GetComponent<KinematicCharacterMotor>());
		}
		if (base.Motor.AttachedRigidbody != null)
		{
			base.Motor.AttachedRigidbody.mass = mass;
		}
		base.Motor.MaxStableSlopeAngle = 70f;
		base.Motor.MaxStableDenivelationAngle = 55f;
		base.Motor.RebuildCollidableLayers();
		if (generateParametersOnAwake)
		{
			GenerateParameters();
		}
		useGravity = gravityParameters.CheckShouldUseGravity();
		isFlying = flightParameters.CheckShouldUseFlight();
		UpdateAuthority();
		this.onMotorStart?.Invoke(body);
	}

	public override void OnStartAuthority()
	{
		UpdateAuthority();
	}

	public override void OnStopAuthority()
	{
		UpdateAuthority();
	}

	private void OnEnable()
	{
		UpdateInnerMotorEnabled();
	}

	private void OnDisable()
	{
		base.Motor.enabled = false;
	}

	private void PreMove(float deltaTime)
	{
		if (!hasEffectiveAuthority)
		{
			return;
		}
		float num = acceleration;
		if (isAirControlForced || !isGrounded)
		{
			num *= (disableAirControlUntilCollision ? 0f : airControl);
		}
		Vector3 vector = moveDirection;
		if (!isFlying)
		{
			vector.y = 0f;
		}
		if (body.isSprinting)
		{
			float magnitude = vector.magnitude;
			if (magnitude < 1f && magnitude > 0f)
			{
				float num2 = 1f / vector.magnitude;
				vector *= num2;
			}
		}
		Vector3 target = vector * walkSpeed;
		if (!isFlying)
		{
			target.y = velocity.y;
		}
		velocity = Vector3.MoveTowards(velocity, target, num * deltaTime);
		if (useGravity)
		{
			ref float y = ref velocity.y;
			y += Physics.gravity.y * deltaTime;
			if (isGrounded)
			{
				y = Mathf.Max(y, 0f);
			}
		}
	}

	public void OnDeathStart()
	{
		alive = false;
	}

	private void FixedUpdate()
	{
		float fixedDeltaTime = Time.fixedDeltaTime;
		if (fixedDeltaTime != 0f)
		{
			Vector3 position = base.transform.position;
			if ((previousPosition - position).sqrMagnitude < 0.00062500004f * fixedDeltaTime)
			{
				restStopwatch += fixedDeltaTime;
			}
			else
			{
				restStopwatch = 0f;
			}
			previousPosition = position;
			if (netIsGrounded)
			{
				lastGroundedTime = Run.FixedTimeStamp.now;
			}
		}
	}

	private void GenerateParameters()
	{
		slopeLimit = 70f;
		stepOffset = Mathf.Min(capsuleHeight * 0.1f, 0.2f);
		stepHandlingMethod = StepHandlingMethod.None;
		ledgeHandling = false;
		interactiveRigidbodyHandling = true;
	}

	public void ApplyForce(Vector3 force, bool alwaysApply = false, bool disableAirControlUntilCollision = false)
	{
		PhysForceInfo forceInfo = PhysForceInfo.Create();
		forceInfo.force = force;
		forceInfo.ignoreGroundStick = alwaysApply;
		forceInfo.disableAirControlUntilCollision = disableAirControlUntilCollision;
		forceInfo.massIsOne = false;
		ApplyForceImpulse(in forceInfo);
	}

	public void ApplyForceImpulse(in PhysForceInfo forceInfo)
	{
		if (NetworkServer.active && !hasEffectiveAuthority)
		{
			CallRpcApplyForceImpulse(forceInfo);
			return;
		}
		Vector3 force = forceInfo.force;
		if (!forceInfo.massIsOne)
		{
			_ = force.magnitude;
			force *= 1f / mass;
		}
		if (mass != 0f)
		{
			if (force.y < 6f && isGrounded && !forceInfo.ignoreGroundStick)
			{
				force.y = 0f;
			}
			if (force.y > 0f)
			{
				base.Motor.ForceUnground();
			}
			velocity += force;
			if (forceInfo.disableAirControlUntilCollision)
			{
				disableAirControlUntilCollision = true;
			}
		}
	}

	[ClientRpc]
	private void RpcApplyForceImpulse(PhysForceInfo physForceInfo)
	{
		if (!NetworkServer.active)
		{
			ApplyForceImpulse(in physForceInfo);
		}
	}

	public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
	{
		currentRotation = Quaternion.identity;
	}

	public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
	{
		currentVelocity = velocity;
	}

	public override void BeforeCharacterUpdate(float deltaTime)
	{
		_ = cvCMotorSafeCollisionStepThreshold.value;
		_ = cvCMotorSafeCollisionStepThreshold.value;
		if (rootMotion != Vector3.zero)
		{
			Vector3 vector = rootMotion;
			rootMotion = Vector3.zero;
			base.Motor.MoveCharacter(base.transform.position + vector);
		}
		PreMove(deltaTime);
		_ = (velocity * deltaTime).sqrMagnitude;
	}

	public override void PostGroundingUpdate(float deltaTime)
	{
		if (base.Motor.GroundingStatus.IsStableOnGround != base.Motor.LastGroundingStatus.IsStableOnGround)
		{
			netIsGrounded = base.Motor.GroundingStatus.IsStableOnGround;
			if (base.Motor.GroundingStatus.IsStableOnGround)
			{
				OnLanded();
			}
			else
			{
				OnLeaveStableGround();
			}
		}
	}

	private void OnLanded()
	{
		jumpCount = 0;
		HitGroundInfo hitGroundInfo = default(HitGroundInfo);
		hitGroundInfo.velocity = lastVelocity;
		hitGroundInfo.position = base.Motor.GroundingStatus.GroundPoint;
		hitGroundInfo.isValidForEffect = Run.FixedTimeStamp.now - lastGroundedTime > 0.2f;
		HitGroundInfo hitGroundInfo2 = hitGroundInfo;
		if (hasEffectiveAuthority)
		{
			try
			{
				this.onHitGroundAuthority?.Invoke(ref hitGroundInfo2);
			}
			catch (Exception message)
			{
				Debug.LogError(message);
			}
			if (NetworkServer.active)
			{
				GlobalEventManager.instance.OnCharacterHitGroundServer(body, hitGroundInfo2);
			}
			else
			{
				CallCmdReportHitGround(hitGroundInfo2);
			}
		}
	}

	[ClientRpc]
	public void RpcOnHitGroundSFX(HitGroundInfo hitGroundInfo, bool tookFallDamage)
	{
		if (!NetworkServer.active)
		{
			GlobalEventManager.instance.OnCharacterHitGroundSFX(body, hitGroundInfo, tookFallDamage);
		}
	}

	[Command]
	private void CmdReportHitGround(HitGroundInfo hitGroundInfo)
	{
		GlobalEventManager.instance.OnCharacterHitGroundServer(body, hitGroundInfo);
	}

	private void OnLeaveStableGround()
	{
		if (jumpCount < 1)
		{
			jumpCount = 1;
		}
		body.SetInLava(b: false);
	}

	public override void AfterCharacterUpdate(float deltaTime)
	{
		lastVelocity = velocity;
		velocity = base.Motor.BaseVelocity;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool IsColliderValidForCollisions(Collider coll)
	{
		if (!coll.isTrigger)
		{
			return coll != base.Motor.Capsule;
		}
		return false;
	}

	public override void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
	{
		isAirControlForced = false;
		SurfaceDef objectSurfaceDef = SurfaceDefProvider.GetObjectSurfaceDef(hitCollider, hitPoint);
		if ((bool)objectSurfaceDef)
		{
			isAirControlForced = objectSurfaceDef.isSlippery;
			body.SetInLava(objectSurfaceDef.isLava);
		}
	}

	public override void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
	{
		disableAirControlUntilCollision = false;
		if (this.onMovementHit != null)
		{
			MovementHitInfo movementHitInfo = default(MovementHitInfo);
			movementHitInfo.velocity = velocity;
			movementHitInfo.hitCollider = hitCollider;
			MovementHitInfo movementHitInfo2 = movementHitInfo;
			this.onMovementHit(ref movementHitInfo2);
			SurfaceDef objectSurfaceDef = SurfaceDefProvider.GetObjectSurfaceDef(hitCollider, hitPoint);
			if ((bool)objectSurfaceDef)
			{
				body.SetInLava(objectSurfaceDef.isLava);
			}
		}
	}

	public override void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
	{
	}

	public void Jump(float horizontalMultiplier, float verticalMultiplier, bool vault = false)
	{
		Vector3 vector = moveDirection;
		if (vault)
		{
			velocity = vector;
		}
		else
		{
			vector.y = 0f;
			float magnitude = vector.magnitude;
			if (magnitude > 0f)
			{
				vector /= magnitude;
			}
			Vector3 vector2 = vector * body.moveSpeed * horizontalMultiplier;
			vector2.y = body.jumpPower * verticalMultiplier;
			velocity = vector2;
		}
		base.Motor.ForceUnground();
	}

	public void AddDisplacement(Vector3 displacement)
	{
		rootMotion += displacement;
	}

	static CharacterMotor()
	{
		enableMotorWithoutAuthority = false;
		cvCMotorSafeCollisionStepThreshold = new FloatConVar("cmotor_safe_collision_step_threshold", ConVarFlags.Cheat, 1.0833334f.ToString(CultureInfo.InvariantCulture), "How large of a movement in meters/fixedTimeStep is needed to trigger more expensive \"safe\" collisions to prevent tunneling.");
		kCmdCmdReportHitGround = 1796547162;
		NetworkBehaviour.RegisterCommandDelegate(typeof(CharacterMotor), kCmdCmdReportHitGround, InvokeCmdCmdReportHitGround);
		kRpcRpcApplyForceImpulse = 1042934326;
		NetworkBehaviour.RegisterRpcDelegate(typeof(CharacterMotor), kRpcRpcApplyForceImpulse, InvokeRpcRpcApplyForceImpulse);
		kRpcRpcOnHitGroundSFX = -1661136052;
		NetworkBehaviour.RegisterRpcDelegate(typeof(CharacterMotor), kRpcRpcOnHitGroundSFX, InvokeRpcRpcOnHitGroundSFX);
		NetworkCRC.RegisterBehaviour("CharacterMotor", 0);
	}

	void IPhysMotor.ApplyForceImpulse(in PhysForceInfo physForceInfo)
	{
		ApplyForceImpulse(in physForceInfo);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdReportHitGround(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdReportHitGround called on client.");
		}
		else
		{
			((CharacterMotor)obj).CmdReportHitGround(GeneratedNetworkCode._ReadHitGroundInfo_CharacterMotor(reader));
		}
	}

	public void CallCmdReportHitGround(HitGroundInfo hitGroundInfo)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdReportHitGround called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdReportHitGround(hitGroundInfo);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdReportHitGround);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WriteHitGroundInfo_CharacterMotor(networkWriter, hitGroundInfo);
		SendCommandInternal(networkWriter, 0, "CmdReportHitGround");
	}

	protected static void InvokeRpcRpcApplyForceImpulse(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcApplyForceImpulse called on server.");
		}
		else
		{
			((CharacterMotor)obj).RpcApplyForceImpulse(GeneratedNetworkCode._ReadPhysForceInfo_None(reader));
		}
	}

	protected static void InvokeRpcRpcOnHitGroundSFX(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcOnHitGroundSFX called on server.");
		}
		else
		{
			((CharacterMotor)obj).RpcOnHitGroundSFX(GeneratedNetworkCode._ReadHitGroundInfo_CharacterMotor(reader), reader.ReadBoolean());
		}
	}

	public void CallRpcApplyForceImpulse(PhysForceInfo physForceInfo)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcApplyForceImpulse called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcApplyForceImpulse);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WritePhysForceInfo_None(networkWriter, physForceInfo);
		SendRPCInternal(networkWriter, 0, "RpcApplyForceImpulse");
	}

	public void CallRpcOnHitGroundSFX(HitGroundInfo hitGroundInfo, bool tookFallDamage)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcOnHitGroundSFX called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcOnHitGroundSFX);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		GeneratedNetworkCode._WriteHitGroundInfo_CharacterMotor(networkWriter, hitGroundInfo);
		networkWriter.Write(tookFallDamage);
		SendRPCInternal(networkWriter, 0, "RpcOnHitGroundSFX");
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool flag = base.OnSerialize(writer, forceAll);
		bool flag2 = default(bool);
		return flag2 || flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		base.OnDeserialize(reader, initialState);
	}

	public override void PreStartClient()
	{
		base.PreStartClient();
	}
}

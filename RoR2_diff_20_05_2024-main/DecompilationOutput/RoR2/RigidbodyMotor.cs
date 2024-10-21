using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
[RequireComponent(typeof(InputBankTest))]
[RequireComponent(typeof(VectorPID))]
public class RigidbodyMotor : NetworkBehaviour, IPhysMotor, IDisplacementReceiver
{
	[HideInInspector]
	public Vector3 moveVector;

	public Rigidbody rigid;

	public VectorPID forcePID;

	public Vector3 centerOfMassOffset;

	public string animatorForward;

	public string animatorRight;

	public string animatorUp;

	public bool enableOverrideMoveVectorInLocalSpace;

	public bool canTakeImpactDamage = true;

	public Vector3 overrideMoveVectorInLocalSpace;

	private NetworkIdentity networkIdentity;

	private CharacterBody characterBody;

	private InputBankTest inputBank;

	private ModelLocator modelLocator;

	private Animator animator;

	private BodyAnimatorSmoothingParameters bodyAnimatorSmoothingParameters;

	private HealthComponent healthComponent;

	private Vector3 rootMotion;

	private const float impactDamageStrength = 0.07f;

	private static int kRpcRpcApplyForceImpulse;

	public bool hasEffectiveAuthority => Util.HasEffectiveAuthority(networkIdentity);

	public float mass => rigid.mass;

	float IPhysMotor.mass => rigid.mass;

	Vector3 IPhysMotor.velocity => rigid.velocity;

	Vector3 IPhysMotor.velocityAuthority
	{
		get
		{
			return rigid.velocity;
		}
		set
		{
			rigid.velocity = value;
		}
	}

	private void Awake()
	{
		networkIdentity = GetComponent<NetworkIdentity>();
		characterBody = GetComponent<CharacterBody>();
		inputBank = GetComponent<InputBankTest>();
		modelLocator = GetComponent<ModelLocator>();
		healthComponent = GetComponent<HealthComponent>();
		bodyAnimatorSmoothingParameters = GetComponent<BodyAnimatorSmoothingParameters>();
	}

	private void Start()
	{
		UpdateAuthority();
		Vector3 centerOfMass = rigid.centerOfMass;
		centerOfMass += centerOfMassOffset;
		rigid.centerOfMass = centerOfMass;
		if ((bool)modelLocator)
		{
			Transform modelTransform = modelLocator.modelTransform;
			if ((bool)modelTransform)
			{
				animator = modelTransform.GetComponent<Animator>();
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(base.transform.position + rigid.centerOfMass, 0.5f);
	}

	public static float GetPitch(Vector3 v)
	{
		float x = Mathf.Sqrt(v.x * v.x + v.z * v.z);
		return 0f - Mathf.Atan2(v.y, x);
	}

	private void Update()
	{
		if ((bool)animator)
		{
			Vector3 vector = base.transform.InverseTransformVector(moveVector) / Mathf.Max(1f, moveVector.magnitude);
			BodyAnimatorSmoothingParameters.SmoothingParameters smoothingParameters = (bodyAnimatorSmoothingParameters ? bodyAnimatorSmoothingParameters.smoothingParameters : BodyAnimatorSmoothingParameters.defaultParameters);
			if (animatorForward.Length > 0)
			{
				animator.SetFloat(animatorForward, vector.z, smoothingParameters.forwardSpeedSmoothDamp, Time.deltaTime);
			}
			if (animatorRight.Length > 0)
			{
				animator.SetFloat(animatorRight, vector.x, smoothingParameters.rightSpeedSmoothDamp, Time.deltaTime);
			}
			if (animatorUp.Length > 0)
			{
				animator.SetFloat(animatorUp, vector.y, smoothingParameters.forwardSpeedSmoothDamp, Time.deltaTime);
			}
		}
	}

	private void FixedUpdate()
	{
		if (!inputBank || !rigid)
		{
			return;
		}
		if ((bool)forcePID)
		{
			if (enableOverrideMoveVectorInLocalSpace)
			{
				moveVector = base.transform.TransformDirection(overrideMoveVectorInLocalSpace) * characterBody.moveSpeed;
			}
			_ = inputBank.aimDirection;
			Vector3 targetVector = moveVector;
			forcePID.inputVector = rigid.velocity;
			forcePID.targetVector = targetVector;
			Debug.DrawLine(base.transform.position, base.transform.position + forcePID.targetVector, Color.red, 0.1f);
			Vector3 vector = forcePID.UpdatePID();
			rigid.AddForceAtPosition(Vector3.ClampMagnitude(vector * (characterBody.acceleration / 3f), characterBody.acceleration), base.transform.position, ForceMode.Acceleration);
		}
		if (rootMotion != Vector3.zero)
		{
			rigid.MovePosition(rigid.position + rootMotion);
			rootMotion = Vector3.zero;
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (canTakeImpactDamage && collision.gameObject.layer == LayerIndex.world.intVal)
		{
			float num = Mathf.Max(characterBody.moveSpeed, characterBody.baseMoveSpeed) * 4f;
			float magnitude = collision.relativeVelocity.magnitude;
			if (magnitude >= num)
			{
				float num2 = magnitude / characterBody.moveSpeed * 0.07f;
				DamageInfo damageInfo = new DamageInfo();
				damageInfo.damage = Mathf.Min(healthComponent.fullCombinedHealth, healthComponent.fullCombinedHealth * num2);
				damageInfo.procCoefficient = 0f;
				damageInfo.position = collision.contacts[0].point;
				damageInfo.attacker = healthComponent.lastHitAttacker;
				healthComponent.TakeDamage(damageInfo);
			}
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

	public void AddDisplacement(Vector3 displacement)
	{
		rootMotion += displacement;
	}

	public void ApplyForceImpulse(in PhysForceInfo forceInfo)
	{
		if (NetworkServer.active && !hasEffectiveAuthority)
		{
			CallRpcApplyForceImpulse(forceInfo);
		}
		else
		{
			rigid.AddForce(forceInfo.force, (!forceInfo.massIsOne) ? ForceMode.Impulse : ForceMode.VelocityChange);
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

	private void UpdateAuthority()
	{
		base.enabled = hasEffectiveAuthority;
	}

	void IPhysMotor.ApplyForceImpulse(in PhysForceInfo physForceInfo)
	{
		ApplyForceImpulse(in physForceInfo);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcApplyForceImpulse(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcApplyForceImpulse called on server.");
		}
		else
		{
			((RigidbodyMotor)obj).RpcApplyForceImpulse(GeneratedNetworkCode._ReadPhysForceInfo_None(reader));
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

	static RigidbodyMotor()
	{
		kRpcRpcApplyForceImpulse = 1386350170;
		NetworkBehaviour.RegisterRpcDelegate(typeof(RigidbodyMotor), kRpcRpcApplyForceImpulse, InvokeRpcRpcApplyForceImpulse);
		NetworkCRC.RegisterBehaviour("RigidbodyMotor", 0);
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

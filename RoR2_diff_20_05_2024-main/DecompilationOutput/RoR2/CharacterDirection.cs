using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class CharacterDirection : NetworkBehaviour, ILifeBehavior
{
	private struct TurnAnimatorParamsSet
	{
		public float angleMin;

		public float angleMax;

		public bool turnRight45;

		public bool turnRight90;

		public bool turnRight135;

		public bool turnLeft45;

		public bool turnLeft90;

		public bool turnLeft135;

		private static readonly int turnRight45ParamHash = Animator.StringToHash("turnRight45");

		private static readonly int turnRight90ParamHash = Animator.StringToHash("turnRight90");

		private static readonly int turnRight135ParamHash = Animator.StringToHash("turnRight135");

		private static readonly int turnLeft45ParamHash = Animator.StringToHash("turnLeft45");

		private static readonly int turnLeft90ParamHash = Animator.StringToHash("turnLeft90");

		private static readonly int turnLeft135ParamHash = Animator.StringToHash("turnLeft135");

		public void Apply(Animator animator)
		{
			animator.SetBool(turnRight45ParamHash, turnRight45);
			animator.SetBool(turnRight90ParamHash, turnRight90);
			animator.SetBool(turnRight135ParamHash, turnRight135);
			animator.SetBool(turnLeft45ParamHash, turnLeft45);
			animator.SetBool(turnLeft90ParamHash, turnLeft90);
			animator.SetBool(turnLeft135ParamHash, turnLeft135);
		}
	}

	[HideInInspector]
	public Vector3 moveVector;

	[Tooltip("The transform to rotate.")]
	public Transform targetTransform;

	[Tooltip("The transform to take the rotation from for animator purposes. Commonly the root node.")]
	public Transform overrideAnimatorForwardTransform;

	public RootMotionAccumulator rootMotionAccumulator;

	public Animator modelAnimator;

	[Tooltip("The character direction is set by root rotation, rather than moveVector.")]
	public bool driveFromRootRotation;

	[Tooltip("The maximum turn rate in degrees/second.")]
	public float turnSpeed = 360f;

	[SerializeField]
	private string turnSoundName;

	private int previousParamsIndex = -1;

	private float yRotationVelocity;

	private float _yaw;

	private Vector3 targetVector = Vector3.zero;

	private const float offset = 22.5f;

	private static readonly TurnAnimatorParamsSet[] turnAnimatorParamsSets = new TurnAnimatorParamsSet[7]
	{
		new TurnAnimatorParamsSet
		{
			angleMin = -180f,
			angleMax = -112.5f,
			turnRight45 = false,
			turnRight90 = false,
			turnRight135 = false,
			turnLeft45 = false,
			turnLeft90 = false,
			turnLeft135 = true
		},
		new TurnAnimatorParamsSet
		{
			angleMin = -112.5f,
			angleMax = -67.5f,
			turnRight45 = false,
			turnRight90 = false,
			turnRight135 = false,
			turnLeft45 = false,
			turnLeft90 = true,
			turnLeft135 = false
		},
		new TurnAnimatorParamsSet
		{
			angleMin = -67.5f,
			angleMax = -22.5f,
			turnRight45 = false,
			turnRight90 = false,
			turnRight135 = false,
			turnLeft45 = true,
			turnLeft90 = false,
			turnLeft135 = false
		},
		new TurnAnimatorParamsSet
		{
			turnRight45 = false,
			turnRight90 = false,
			turnRight135 = false,
			turnLeft45 = false,
			turnLeft90 = false,
			turnLeft135 = false
		},
		new TurnAnimatorParamsSet
		{
			angleMin = 22.5f,
			angleMax = 67.5f,
			turnRight45 = true,
			turnRight90 = false,
			turnRight135 = false,
			turnLeft45 = false,
			turnLeft90 = false,
			turnLeft135 = false
		},
		new TurnAnimatorParamsSet
		{
			angleMin = 67.5f,
			angleMax = 112.5f,
			turnRight45 = false,
			turnRight90 = true,
			turnRight135 = false,
			turnLeft45 = false,
			turnLeft90 = false,
			turnLeft135 = false
		},
		new TurnAnimatorParamsSet
		{
			angleMin = 112.5f,
			angleMax = 180f,
			turnRight45 = false,
			turnRight90 = false,
			turnRight135 = true,
			turnLeft45 = false,
			turnLeft90 = false,
			turnLeft135 = false
		}
	};

	private static readonly int paramsMidIndex = turnAnimatorParamsSets.Length >> 1;

	public float yaw
	{
		get
		{
			return _yaw;
		}
		set
		{
			_yaw = value;
			if ((bool)targetTransform)
			{
				targetTransform.rotation = Quaternion.Euler(0f, _yaw, 0f);
			}
		}
	}

	public Vector3 animatorForward
	{
		get
		{
			if (!overrideAnimatorForwardTransform)
			{
				return forward;
			}
			float y = overrideAnimatorForwardTransform.eulerAngles.y;
			return Quaternion.Euler(0f, y, 0f) * Vector3.forward;
		}
	}

	public Vector3 forward
	{
		get
		{
			return Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
		}
		set
		{
			value.y = 0f;
			yaw = Util.QuaternionSafeLookRotation(value, Vector3.up).eulerAngles.y;
		}
	}

	public bool hasEffectiveAuthority { get; private set; }

	private void UpdateAuthority()
	{
		hasEffectiveAuthority = Util.HasEffectiveAuthority(base.gameObject);
	}

	public override void OnStartAuthority()
	{
		UpdateAuthority();
	}

	public override void OnStopAuthority()
	{
		UpdateAuthority();
	}

	private void Start()
	{
		UpdateAuthority();
		ModelLocator component = GetComponent<ModelLocator>();
		if ((bool)component)
		{
			modelAnimator = component.modelTransform.GetComponent<Animator>();
		}
	}

	private void Update()
	{
		Simulate(Time.deltaTime);
	}

	public void OnDeathStart()
	{
		base.enabled = false;
	}

	private static int PickIndex(float angle)
	{
		float num = Mathf.Sign(angle);
		int num2 = Mathf.CeilToInt((angle * num - 22.5f) * (1f / 45f));
		return Mathf.Clamp(paramsMidIndex + num2 * (int)num, 0, turnAnimatorParamsSets.Length - 1);
	}

	private void Simulate(float deltaTime)
	{
		Quaternion quaternion = Quaternion.Euler(0f, yaw, 0f);
		if (!hasEffectiveAuthority)
		{
			return;
		}
		if (driveFromRootRotation)
		{
			Quaternion quaternion2 = rootMotionAccumulator.ExtractRootRotation();
			if ((bool)targetTransform)
			{
				targetTransform.rotation = quaternion * quaternion2;
				float y = targetTransform.rotation.eulerAngles.y;
				yaw = y;
				float angle = 0f;
				if (moveVector.sqrMagnitude > 0f)
				{
					angle = Util.AngleSigned(Vector3.ProjectOnPlane(moveVector, Vector3.up), targetTransform.forward, -Vector3.up);
				}
				int num = PickIndex(angle);
				if (turnSoundName != null && num != previousParamsIndex)
				{
					Util.PlaySound(turnSoundName, base.gameObject);
				}
				previousParamsIndex = num;
				turnAnimatorParamsSets[num].Apply(modelAnimator);
			}
		}
		targetVector = moveVector;
		targetVector.y = 0f;
		if (targetVector != Vector3.zero && deltaTime != 0f)
		{
			targetVector.Normalize();
			Quaternion quaternion3 = Util.QuaternionSafeLookRotation(targetVector, Vector3.up);
			float y2 = Mathf.SmoothDampAngle(yaw, quaternion3.eulerAngles.y, ref yRotationVelocity, 360f / turnSpeed * 0.25f, float.PositiveInfinity, deltaTime);
			quaternion = Quaternion.Euler(0f, y2, 0f);
			yaw = y2;
		}
		if ((bool)targetTransform)
		{
			targetTransform.rotation = quaternion;
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

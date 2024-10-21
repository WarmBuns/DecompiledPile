using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(Animator))]
public class AimAnimator : MonoBehaviour, ILifeBehavior
{
	public enum AimType
	{
		Direct,
		Smart
	}

	public class DirectionOverrideRequest : IDisposable
	{
		public readonly Func<Vector3> directionGetter;

		private Action<DirectionOverrideRequest> disposeCallback;

		public DirectionOverrideRequest(Func<Vector3> getter, Action<DirectionOverrideRequest> onDispose)
		{
			disposeCallback = onDispose;
			directionGetter = getter;
		}

		public void Dispose()
		{
			disposeCallback?.Invoke(this);
			disposeCallback = null;
		}
	}

	private struct AimAngles
	{
		public float pitch;

		public float yaw;
	}

	[Tooltip("The input bank component of the character.")]
	public InputBankTest inputBank;

	[Tooltip("The direction component of the character.")]
	public CharacterDirection directionComponent;

	[Tooltip("The minimum pitch supplied by the aiming animation.")]
	public float pitchRangeMin;

	[Tooltip("The maximum pitch supplied by the aiming animation.")]
	public float pitchRangeMax;

	[Tooltip("The minimum yaw supplied by the aiming animation.")]
	public float yawRangeMin;

	[Tooltip("The maximum yaw supplied by the aiming animation.")]
	public float yawRangeMax;

	[Tooltip("If the pitch is this many degrees beyond the range the aiming animations support, the character will return to neutral pose after waiting the giveup duration.")]
	public float pitchGiveupRange;

	[Tooltip("If the yaw is this many degrees beyond the range the aiming animations support, the character will return to neutral pose after waiting the giveup duration.")]
	public float yawGiveupRange;

	[Tooltip("If the pitch or yaw exceed the range supported by the aiming animations, the character will return to neutral pose after waiting this long.")]
	public float giveupDuration;

	[Tooltip("The speed in degrees/second to approach the desired pitch/yaw by while the weapon should be raised.")]
	public float raisedApproachSpeed = 720f;

	[Tooltip("The speed in degrees/second to approach the desired pitch/yaw by while the weapon should be lowered.")]
	public float loweredApproachSpeed = 360f;

	[Tooltip("The smoothing time for the motion.")]
	public float smoothTime = 0.1f;

	[Tooltip("Whether or not the character can do full 360 yaw turns.")]
	public bool fullYaw;

	[Tooltip("Switches between Direct (point straight at target) or Smart (only turn when outside angle range).")]
	public AimType aimType;

	[Tooltip("Assigns the weight of the aim from the center as an animator value 'aimWeight' between 0-1.")]
	public bool enableAimWeight;

	public bool UseTransformedAimVector;

	private Animator animatorComponent;

	private float pitchClipCycleEnd;

	private float yawClipCycleEnd;

	private float giveupTimer;

	private AimAngles localAnglesToAimVector;

	private AimAngles overshootAngles;

	private AimAngles clampedLocalAnglesToAimVector;

	private AimAngles currentLocalAngles;

	private AimAngles smoothingVelocity;

	private List<DirectionOverrideRequest> directionOverrideRequests = new List<DirectionOverrideRequest>();

	private static readonly int aimPitchCycleHash = Animator.StringToHash("aimPitchCycle");

	private static readonly int aimYawCycleHash = Animator.StringToHash("aimYawCycle");

	private static readonly int aimWeightHash = Animator.StringToHash("aimWeight");

	public bool isOutsideOfRange { get; private set; }

	private bool shouldGiveup => giveupTimer <= 0f;

	public DirectionOverrideRequest RequestDirectionOverride(Func<Vector3> getter)
	{
		DirectionOverrideRequest directionOverrideRequest = new DirectionOverrideRequest(getter, RemoveRequest);
		directionOverrideRequests.Add(directionOverrideRequest);
		return directionOverrideRequest;
	}

	private void RemoveRequest(DirectionOverrideRequest request)
	{
		directionOverrideRequests.Remove(request);
	}

	private void Awake()
	{
		animatorComponent = GetComponent<Animator>();
	}

	private void Start()
	{
		int layerIndex = animatorComponent.GetLayerIndex("AimPitch");
		int layerIndex2 = animatorComponent.GetLayerIndex("AimYaw");
		animatorComponent.Play("PitchControl", layerIndex);
		animatorComponent.Play("YawControl", layerIndex2);
		animatorComponent.Update(0f);
		AnimatorClipInfo[] currentAnimatorClipInfo = animatorComponent.GetCurrentAnimatorClipInfo(layerIndex);
		AnimatorClipInfo[] currentAnimatorClipInfo2 = animatorComponent.GetCurrentAnimatorClipInfo(layerIndex2);
		if (currentAnimatorClipInfo.Length != 0)
		{
			AnimationClip clip = currentAnimatorClipInfo[0].clip;
			double num = clip.length * clip.frameRate;
			pitchClipCycleEnd = (float)((num - 1.0) / num);
		}
		if (currentAnimatorClipInfo2.Length != 0)
		{
			AnimationClip clip2 = currentAnimatorClipInfo2[0].clip;
			_ = clip2.length;
			_ = clip2.frameRate;
			yawClipCycleEnd = 0.999f;
		}
	}

	private void Update()
	{
		if (!(Time.deltaTime <= 0f))
		{
			UpdateLocalAnglesToAimVector();
			UpdateGiveup();
			ApproachDesiredAngles();
			UpdateAnimatorParameters(animatorComponent, pitchRangeMin, pitchRangeMax, yawRangeMin, yawRangeMax);
		}
	}

	public void OnDeathStart()
	{
		base.enabled = false;
		currentLocalAngles = new AimAngles
		{
			pitch = 0f,
			yaw = 0f
		};
		UpdateAnimatorParameters(animatorComponent, pitchRangeMin, pitchRangeMax, yawRangeMin, yawRangeMax);
	}

	private static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
	{
		return outMin + (value - inMin) / (inMax - inMin) * (outMax - outMin);
	}

	private static float NormalizeAngle(float angle)
	{
		return Mathf.Repeat(angle + 180f, 360f) - 180f;
	}

	private void UpdateLocalAnglesToAimVector()
	{
		Vector3 vector = ((directionOverrideRequests.Count <= 0) ? (inputBank ? inputBank.aimDirection : base.transform.forward) : directionOverrideRequests[directionOverrideRequests.Count - 1].directionGetter());
		if (UseTransformedAimVector)
		{
			Vector3 eulerAngles = Util.QuaternionSafeLookRotation(base.transform.InverseTransformDirection(vector), base.transform.up).eulerAngles;
			localAnglesToAimVector = new AimAngles
			{
				pitch = NormalizeAngle(eulerAngles.x),
				yaw = NormalizeAngle(eulerAngles.y)
			};
		}
		else
		{
			float y = (directionComponent ? directionComponent.yaw : base.transform.eulerAngles.y);
			float x = (directionComponent ? directionComponent.transform.eulerAngles.x : base.transform.eulerAngles.x);
			float z = (directionComponent ? directionComponent.transform.eulerAngles.z : base.transform.eulerAngles.z);
			Vector3 eulerAngles2 = Util.QuaternionSafeLookRotation(vector, base.transform.up).eulerAngles;
			Vector3 vector2 = vector;
			Vector3 vector3 = new Vector3(x, y, z);
			vector2.y = 0f;
			localAnglesToAimVector = new AimAngles
			{
				pitch = (0f - Mathf.Atan2(vector.y, vector2.magnitude)) * 57.29578f,
				yaw = NormalizeAngle(eulerAngles2.y - vector3.y)
			};
		}
		overshootAngles = new AimAngles
		{
			pitch = Mathf.Max(pitchRangeMin - localAnglesToAimVector.pitch, localAnglesToAimVector.pitch - pitchRangeMax),
			yaw = Mathf.Max(Mathf.DeltaAngle(localAnglesToAimVector.yaw, yawRangeMin), Mathf.DeltaAngle(yawRangeMax, localAnglesToAimVector.yaw))
		};
		clampedLocalAnglesToAimVector = new AimAngles
		{
			pitch = Mathf.Clamp(localAnglesToAimVector.pitch, pitchRangeMin, pitchRangeMax),
			yaw = Mathf.Clamp(localAnglesToAimVector.yaw, yawRangeMin, yawRangeMax)
		};
	}

	private void ApproachDesiredAngles()
	{
		AimAngles aimAngles2;
		float maxSpeed;
		if (shouldGiveup)
		{
			AimAngles aimAngles = default(AimAngles);
			aimAngles.pitch = 0f;
			aimAngles.yaw = 0f;
			aimAngles2 = aimAngles;
			maxSpeed = loweredApproachSpeed;
		}
		else
		{
			aimAngles2 = clampedLocalAnglesToAimVector;
			maxSpeed = raisedApproachSpeed;
		}
		float yaw = ((!fullYaw) ? Mathf.SmoothDamp(currentLocalAngles.yaw, aimAngles2.yaw, ref smoothingVelocity.yaw, smoothTime, maxSpeed, Time.deltaTime) : NormalizeAngle(Mathf.SmoothDampAngle(currentLocalAngles.yaw, aimAngles2.yaw, ref smoothingVelocity.yaw, smoothTime, maxSpeed, Time.deltaTime)));
		currentLocalAngles = new AimAngles
		{
			pitch = Mathf.SmoothDampAngle(currentLocalAngles.pitch, aimAngles2.pitch, ref smoothingVelocity.pitch, smoothTime, maxSpeed, Time.deltaTime),
			yaw = yaw
		};
	}

	private void ResetGiveup()
	{
		giveupTimer = giveupDuration;
	}

	private void UpdateGiveup()
	{
		if (overshootAngles.pitch > pitchGiveupRange || (!fullYaw && overshootAngles.yaw > yawGiveupRange))
		{
			giveupTimer -= Time.deltaTime;
			isOutsideOfRange = true;
		}
		else
		{
			isOutsideOfRange = false;
			ResetGiveup();
		}
	}

	public void AimImmediate()
	{
		UpdateLocalAnglesToAimVector();
		ResetGiveup();
		currentLocalAngles = clampedLocalAnglesToAimVector;
		smoothingVelocity = new AimAngles
		{
			pitch = 0f,
			yaw = 0f
		};
		UpdateAnimatorParameters(animatorComponent, pitchRangeMin, pitchRangeMax, yawRangeMin, yawRangeMax);
	}

	public void UpdateAnimatorParameters(Animator animator, float pitchRangeMin, float pitchRangeMax, float yawRangeMin, float yawRangeMax)
	{
		float num = 1f;
		if (enableAimWeight)
		{
			num = animatorComponent.GetFloat(aimWeightHash);
		}
		animator.SetFloat(aimPitchCycleHash, Remap(currentLocalAngles.pitch * num, pitchRangeMin, pitchRangeMax, pitchClipCycleEnd, 0f));
		animator.SetFloat(aimYawCycleHash, Remap(currentLocalAngles.yaw * num, yawRangeMin, yawRangeMax, 0f, yawClipCycleEnd));
	}
}

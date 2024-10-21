using System.Runtime.CompilerServices;
using RoR2;
using UnityEngine;

namespace EntityStates;

public class BaseCharacterMain : BaseState
{
	private RootMotionAccumulator rootMotionAccumulator;

	private Vector3 previousPosition;

	protected CharacterAnimParamAvailability characterAnimParamAvailability;

	private CharacterAnimatorWalkParamCalculator animatorWalkParamCalculator;

	protected BodyAnimatorSmoothingParameters.SmoothingParameters smoothingParameters;

	protected bool useRootMotion;

	private bool wasGrounded;

	private float lastYSpeed;

	protected bool hasCharacterMotor;

	protected bool hasCharacterDirection;

	protected bool hasCharacterBody;

	protected bool hasRailMotor;

	protected bool hasCameraTargetParams;

	protected bool hasSkillLocator;

	protected bool hasModelAnimator;

	protected bool hasInputBank;

	protected bool hasRootMotionAccumulator;

	protected Animator modelAnimator { get; private set; }

	protected Vector3 estimatedVelocity { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
		rootMotionAccumulator = GetModelRootMotionAccumulator();
		if ((bool)rootMotionAccumulator)
		{
			rootMotionAccumulator.ExtractRootMotion();
		}
		GetBodyAnimatorSmoothingParameters(out smoothingParameters);
		previousPosition = base.transform.position;
		hasCharacterMotor = base.characterMotor;
		hasCharacterDirection = base.characterDirection;
		hasCharacterBody = base.characterBody;
		hasRailMotor = base.railMotor;
		hasCameraTargetParams = base.cameraTargetParams;
		hasSkillLocator = base.skillLocator;
		hasModelAnimator = modelAnimator;
		hasInputBank = base.inputBank;
		hasRootMotionAccumulator = rootMotionAccumulator;
		if ((bool)modelAnimator)
		{
			characterAnimParamAvailability = CharacterAnimParamAvailability.FromAnimator(modelAnimator);
			int layerIndex = modelAnimator.GetLayerIndex("Body");
			if (characterAnimParamAvailability.isGrounded)
			{
				wasGrounded = base.isGrounded;
				modelAnimator.SetBool(AnimationParameters.isGrounded, wasGrounded);
			}
			if (base.isGrounded || !hasCharacterMotor)
			{
				modelAnimator.CrossFadeInFixedTime("Idle", 0.1f, layerIndex);
			}
			else
			{
				modelAnimator.CrossFadeInFixedTime("AscendDescend", 0.1f, layerIndex);
			}
			modelAnimator.Update(0f);
		}
	}

	public override void OnExit()
	{
		if ((bool)rootMotionAccumulator)
		{
			rootMotionAccumulator.ExtractRootMotion();
		}
		if ((bool)modelAnimator)
		{
			if (characterAnimParamAvailability.isMoving)
			{
				modelAnimator.SetBool(AnimationParameters.isMoving, value: false);
			}
			if (characterAnimParamAvailability.turnAngle)
			{
				modelAnimator.SetFloat(AnimationParameters.turnAngle, 0f);
			}
		}
		base.OnExit();
	}

	public override void Update()
	{
		base.Update();
		if (!(Time.deltaTime <= 0f))
		{
			UpdateEstimatedVelocity();
			useRootMotion = ((bool)base.characterBody && base.characterBody.rootMotionInMainState && base.isGrounded) || (bool)base.railMotor;
			UpdateAnimationParameters();
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void UpdateEstimatedVelocity()
	{
		Vector3 vector;
		if (hasCharacterMotor)
		{
			vector = base.characterMotor.velocity;
		}
		else if (base.rigidbodyMotor != null && base.rigidbody != null)
		{
			vector = base.rigidbody.velocity;
		}
		else
		{
			Vector3 position = base.transform.position;
			vector = (position - previousPosition) / Time.deltaTime;
			previousPosition = position;
		}
		estimatedVelocity = (estimatedVelocity + vector + vector) * (1f / 3f);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (hasCharacterMotor)
		{
			float num = estimatedVelocity.y - lastYSpeed;
			if (base.isGrounded && !wasGrounded && hasModelAnimator)
			{
				int layerIndex = modelAnimator.GetLayerIndex("Impact");
				if (layerIndex >= 0)
				{
					modelAnimator.SetLayerWeight(layerIndex, Mathf.Clamp01(Mathf.Max(0.3f, num / 5f, modelAnimator.GetLayerWeight(layerIndex))));
					modelAnimator.PlayInFixedTime("LightImpact", layerIndex, 0f);
				}
			}
			wasGrounded = base.isGrounded;
			lastYSpeed = estimatedVelocity.y;
		}
		if (!hasRootMotionAccumulator)
		{
			return;
		}
		Vector3 vector = rootMotionAccumulator.ExtractRootMotion();
		if (useRootMotion && vector != Vector3.zero && base.isAuthority)
		{
			if ((bool)base.characterMotor)
			{
				base.characterMotor.rootMotion += vector;
			}
			if ((bool)base.railMotor)
			{
				base.railMotor.rootMotion += vector;
			}
		}
	}

	protected virtual void UpdateAnimationParameters()
	{
		if (hasRailMotor || !hasModelAnimator)
		{
			return;
		}
		float deltaTime = GetDeltaTime();
		Vector3 vector = (base.inputBank ? base.inputBank.moveVector : Vector3.zero);
		bool value = vector != Vector3.zero && base.characterBody.moveSpeed > Mathf.Epsilon;
		animatorWalkParamCalculator.Update(vector, base.characterDirection ? base.characterDirection.animatorForward : base.transform.forward, in smoothingParameters, deltaTime);
		if (useRootMotion)
		{
			if (characterAnimParamAvailability.mainRootPlaybackRate)
			{
				float num = 1f;
				if ((bool)base.modelLocator && (bool)base.modelLocator.modelTransform)
				{
					num = base.modelLocator.modelTransform.localScale.x;
				}
				float value2 = base.characterBody.moveSpeed / (base.characterBody.mainRootSpeed * num);
				modelAnimator.SetFloat(AnimationParameters.mainRootPlaybackRate, value2);
			}
		}
		else if (characterAnimParamAvailability.walkSpeed)
		{
			modelAnimator.SetFloat(AnimationParameters.walkSpeed, base.characterBody.moveSpeed);
		}
		if (characterAnimParamAvailability.isGrounded)
		{
			modelAnimator.SetBool(AnimationParameters.isGrounded, base.isGrounded);
		}
		if (characterAnimParamAvailability.isMoving)
		{
			modelAnimator.SetBool(AnimationParameters.isMoving, value);
		}
		if (characterAnimParamAvailability.turnAngle)
		{
			modelAnimator.SetFloat(AnimationParameters.turnAngle, animatorWalkParamCalculator.remainingTurnAngle, smoothingParameters.turnAngleSmoothDamp, deltaTime);
		}
		if (characterAnimParamAvailability.isSprinting)
		{
			modelAnimator.SetBool(AnimationParameters.isSprinting, base.characterBody.isSprinting);
		}
		if (characterAnimParamAvailability.forwardSpeed)
		{
			modelAnimator.SetFloat(AnimationParameters.forwardSpeed, animatorWalkParamCalculator.animatorWalkSpeed.x, smoothingParameters.forwardSpeedSmoothDamp, Time.deltaTime);
		}
		if (characterAnimParamAvailability.rightSpeed)
		{
			modelAnimator.SetFloat(AnimationParameters.rightSpeed, animatorWalkParamCalculator.animatorWalkSpeed.y, smoothingParameters.rightSpeedSmoothDamp, Time.deltaTime);
		}
		if (characterAnimParamAvailability.upSpeed)
		{
			modelAnimator.SetFloat(AnimationParameters.upSpeed, estimatedVelocity.y, 0.1f, Time.deltaTime);
		}
	}
}

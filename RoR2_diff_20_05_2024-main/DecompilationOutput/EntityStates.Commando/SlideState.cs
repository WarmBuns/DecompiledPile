using RoR2;
using UnityEngine;

namespace EntityStates.Commando;

public class SlideState : BaseState
{
	public static float slideDuration;

	public static float jumpDuration;

	public static AnimationCurve forwardSpeedCoefficientCurve;

	public static AnimationCurve jumpforwardSpeedCoefficientCurve;

	public static string soundString;

	public static GameObject jetEffectPrefab;

	public static GameObject slideEffectPrefab;

	private Vector3 forwardDirection;

	private GameObject slideEffectInstance;

	private bool startedStateGrounded;

	private static int JumpStateHash = Animator.StringToHash("Jump");

	private static int SlideForwardStateHash = Animator.StringToHash("SlideForward");

	private static int SlideForwardParamHash = Animator.StringToHash("SlideForward.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(soundString, base.gameObject);
		if ((bool)base.inputBank && (bool)base.characterDirection)
		{
			base.characterDirection.forward = ((base.inputBank.moveVector == Vector3.zero) ? base.characterDirection.forward : base.inputBank.moveVector).normalized;
		}
		if ((bool)base.characterMotor)
		{
			startedStateGrounded = base.characterMotor.isGrounded;
		}
		if ((bool)jetEffectPrefab)
		{
			Transform transform = FindModelChild("LeftJet");
			Transform transform2 = FindModelChild("RightJet");
			if ((bool)transform)
			{
				Object.Instantiate(jetEffectPrefab, transform);
			}
			if ((bool)transform2)
			{
				Object.Instantiate(jetEffectPrefab, transform2);
			}
		}
		base.characterBody.SetSpreadBloom(0f, canOnlyIncreaseBloom: false);
		if (!startedStateGrounded)
		{
			PlayAnimation("Body", JumpStateHash);
			Vector3 velocity = base.characterMotor.velocity;
			velocity.y = base.characterBody.jumpPower;
			base.characterMotor.velocity = velocity;
			return;
		}
		PlayAnimation("Body", SlideForwardStateHash, SlideForwardParamHash, slideDuration);
		if ((bool)slideEffectPrefab)
		{
			Transform parent = FindModelChild("Base");
			slideEffectInstance = Object.Instantiate(slideEffectPrefab, parent);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			float num = (startedStateGrounded ? slideDuration : jumpDuration);
			if ((bool)base.inputBank && (bool)base.characterDirection)
			{
				base.characterDirection.moveVector = base.inputBank.moveVector;
				forwardDirection = base.characterDirection.forward;
			}
			if ((bool)base.characterMotor)
			{
				float num2 = 0f;
				num2 = ((!startedStateGrounded) ? jumpforwardSpeedCoefficientCurve.Evaluate(base.fixedAge / num) : forwardSpeedCoefficientCurve.Evaluate(base.fixedAge / num));
				base.characterMotor.rootMotion += num2 * moveSpeedStat * forwardDirection * GetDeltaTime();
			}
			if (base.fixedAge >= num)
			{
				outer.SetNextStateToMain();
			}
		}
	}

	public override void OnExit()
	{
		PlayImpactAnimation();
		if ((bool)slideEffectInstance)
		{
			EntityState.Destroy(slideEffectInstance);
		}
		base.OnExit();
	}

	private void PlayImpactAnimation()
	{
		Animator modelAnimator = GetModelAnimator();
		int layerIndex = modelAnimator.GetLayerIndex("Impact");
		if (layerIndex >= 0)
		{
			modelAnimator.SetLayerWeight(layerIndex, 1f);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

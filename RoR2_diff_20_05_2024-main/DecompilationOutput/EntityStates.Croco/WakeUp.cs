using RoR2;
using UnityEngine;

namespace EntityStates.Croco;

public class WakeUp : BaseState
{
	public static float duration;

	public static float delayBeforeAimAnimatorWeight;

	private Animator modelAnimator;

	private static int SleepToIdleStateHash = Animator.StringToHash("SleepToIdle");

	private static int SleepToIdleParamHash = Animator.StringToHash("SleepToIdle.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		base.modelLocator.normalizeToFloor = true;
		modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			modelAnimator.SetFloat(AnimationParameters.aimWeight, 0f);
		}
		PlayAnimation("Body", SleepToIdleStateHash, SleepToIdleParamHash, duration);
	}

	public override void Update()
	{
		base.Update();
		if ((bool)modelAnimator)
		{
			modelAnimator.SetFloat(AnimationParameters.aimWeight, Mathf.Clamp01((base.age - delayBeforeAimAnimatorWeight) / duration));
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		if ((bool)modelAnimator)
		{
			modelAnimator.SetFloat(AnimationParameters.aimWeight, 1f);
		}
		base.OnExit();
	}
}

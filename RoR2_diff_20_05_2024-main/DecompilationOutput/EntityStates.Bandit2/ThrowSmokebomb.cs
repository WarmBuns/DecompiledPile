using UnityEngine;

namespace EntityStates.Bandit2;

public class ThrowSmokebomb : BaseState
{
	public static float duration;

	private static int throwSmokeBombStateHash = Animator.StringToHash("ThrowSmokebomb");

	private static int throwSmokeBombParamHash = Animator.StringToHash("ThrowSmokebomb.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Gesture, Additive", throwSmokeBombStateHash, throwSmokeBombParamHash, duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextState(new StealthMode());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

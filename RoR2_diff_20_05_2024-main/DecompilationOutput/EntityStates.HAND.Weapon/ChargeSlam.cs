using UnityEngine;

namespace EntityStates.HAND.Weapon;

public class ChargeSlam : BaseState
{
	public static float baseDuration = 3.5f;

	private float duration;

	private Animator modelAnimator;

	private static int ChargeSlamStateHash = Animator.StringToHash("ChargeSlam");

	private static int ChargeSlamParamHash = Animator.StringToHash("ChargeSlam.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			PlayAnimation("Gesture", ChargeSlamStateHash, ChargeSlamParamHash, duration);
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(4f);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.characterMotor.isGrounded && base.isAuthority)
		{
			outer.SetNextState(new Slam());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

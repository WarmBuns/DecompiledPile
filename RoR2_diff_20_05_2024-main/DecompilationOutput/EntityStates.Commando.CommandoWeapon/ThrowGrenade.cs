using UnityEngine;

namespace EntityStates.Commando.CommandoWeapon;

public class ThrowGrenade : GenericProjectileBaseState
{
	private static int ThrowGrenadeStateHash = Animator.StringToHash("ThrowGrenade");

	private static int FireFMJParamHash = Animator.StringToHash("FireFMJ.playbackRate");

	protected override void PlayAnimation(float duration)
	{
		if ((bool)GetModelAnimator())
		{
			PlayAnimation("Gesture, Additive", ThrowGrenadeStateHash, FireFMJParamHash, duration * 2f);
			PlayAnimation("Gesture, Override", ThrowGrenadeStateHash, FireFMJParamHash, duration * 2f);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

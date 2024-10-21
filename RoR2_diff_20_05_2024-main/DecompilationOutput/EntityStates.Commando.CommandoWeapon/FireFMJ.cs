using RoR2;
using UnityEngine;

namespace EntityStates.Commando.CommandoWeapon;

public class FireFMJ : GenericProjectileBaseState
{
	private static int FireFMJStateHash = Animator.StringToHash("FireFMJ");

	private static int FireFMJParamHash = Animator.StringToHash("FireFMJ.playbackRate");

	protected override void PlayAnimation(float duration)
	{
		base.PlayAnimation(duration);
		if ((bool)GetModelAnimator())
		{
			PlayAnimation("Gesture, Additive", FireFMJStateHash, FireFMJParamHash, duration);
			PlayAnimation("Gesture, Override", FireFMJStateHash, FireFMJParamHash, duration);
		}
	}

	protected override Ray ModifyProjectileAimRay(Ray aimRay)
	{
		TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref aimRay, projectilePrefab, base.gameObject);
		return aimRay;
	}
}

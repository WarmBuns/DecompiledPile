using RoR2;
using UnityEngine;

namespace EntityStates.Merc.Weapon;

public class ThrowEvisProjectile : GenericProjectileBaseState
{
	public static float shortHopVelocity;

	private static int GroundLight3StateHash = Animator.StringToHash("GroundLight3");

	private static int GroundLightParamHash = Animator.StringToHash("GroundLight.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.characterMotor)
		{
			base.characterMotor.velocity.y = Mathf.Max(base.characterMotor.velocity.y, shortHopVelocity);
		}
	}

	protected override void PlayAnimation(float duration)
	{
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			bool @bool = modelAnimator.GetBool("isMoving");
			bool bool2 = modelAnimator.GetBool("isGrounded");
			if (@bool || !bool2)
			{
				PlayAnimation("Gesture, Additive", GroundLight3StateHash, GroundLightParamHash, duration);
				PlayAnimation("Gesture, Override", GroundLight3StateHash, GroundLightParamHash, duration);
			}
			else
			{
				PlayAnimation("FullBody, Override", GroundLight3StateHash, GroundLightParamHash, duration);
			}
		}
	}

	protected override Ray ModifyProjectileAimRay(Ray aimRay)
	{
		TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref aimRay, projectilePrefab, base.gameObject);
		return aimRay;
	}
}

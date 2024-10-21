using UnityEngine;

namespace EntityStates.Commando.CommandoWeapon;

public class FireShotgunBlast : GenericBulletBaseState
{
	public static float delayBetweenShotgunBlasts;

	private bool hasFiredSecondBlast;

	private static int FirePistolLeftStateHash = Animator.StringToHash("FirePistol, Left");

	private static int FirePistolRightStateHash = Animator.StringToHash("FirePistol, Right");

	public override void OnEnter()
	{
		muzzleName = "MuzzleLeft";
		base.OnEnter();
		PlayAnimation("Gesture Additive, Left", FirePistolLeftStateHash);
		PlayAnimation("Gesture Override, Left", FirePistolLeftStateHash);
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(2f);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!hasFiredSecondBlast && delayBetweenShotgunBlasts / attackSpeedStat < base.fixedAge)
		{
			hasFiredSecondBlast = true;
			PlayAnimation("Gesture Additive, Right", FirePistolRightStateHash);
			PlayAnimation("Gesture Override, Right", FirePistolRightStateHash);
			muzzleName = "MuzzleRight";
			FireBullet(GetAimRay());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

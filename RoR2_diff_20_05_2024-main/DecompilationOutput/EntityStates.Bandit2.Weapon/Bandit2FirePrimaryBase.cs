using RoR2;
using UnityEngine;

namespace EntityStates.Bandit2.Weapon;

public abstract class Bandit2FirePrimaryBase : GenericBulletBaseState
{
	[SerializeField]
	public float minimumBaseDuration;

	protected float minimumDuration;

	private static int FireMainWeaponStateHash = Animator.StringToHash("FireMainWeapon");

	private static int FireMainWeaponParamHash = Animator.StringToHash("FireMainWeapon.playbackRate");

	public override void OnEnter()
	{
		minimumDuration = minimumBaseDuration / attackSpeedStat;
		base.OnEnter();
		PlayAnimation("Gesture, Additive", FireMainWeaponStateHash, FireMainWeaponParamHash, duration);
	}

	protected override void ModifyBullet(BulletAttack bulletAttack)
	{
		base.ModifyBullet(bulletAttack);
		bulletAttack.falloffModel = BulletAttack.FalloffModel.DefaultBullet;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		if (!(base.fixedAge > minimumDuration))
		{
			return InterruptPriority.Skill;
		}
		return InterruptPriority.Any;
	}
}

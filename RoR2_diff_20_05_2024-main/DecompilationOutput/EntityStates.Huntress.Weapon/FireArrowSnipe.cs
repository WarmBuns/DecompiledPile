using RoR2;
using UnityEngine;

namespace EntityStates.Huntress.Weapon;

public class FireArrowSnipe : GenericBulletBaseState
{
	public float charge;

	public static float recoilAmplitude;

	private static int FireArrowSnipeStateHash = Animator.StringToHash("FireArrowSnipe");

	private static int FireArrowSnipeParamHash = Animator.StringToHash("FireArrowSnipe.playbackRate");

	protected override void ModifyBullet(BulletAttack bulletAttack)
	{
		base.ModifyBullet(bulletAttack);
		bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
		_ = (bool)(base.skillLocator ? base.skillLocator.primary : null);
	}

	protected override void FireBullet(Ray aimRay)
	{
		base.FireBullet(aimRay);
		base.characterBody.SetSpreadBloom(0.2f, canOnlyIncreaseBloom: false);
		AddRecoil(-0.6f * recoilAmplitude, -0.8f * recoilAmplitude, -0.1f * recoilAmplitude, 0.1f * recoilAmplitude);
		PlayAnimation("Body", FireArrowSnipeStateHash, FireArrowSnipeParamHash, duration);
		base.healthComponent.TakeDamageForce(aimRay.direction * -400f, alwaysApply: true);
	}
}

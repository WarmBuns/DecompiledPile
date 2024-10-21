using RoR2;
using UnityEngine;

namespace EntityStates.Captain.Weapon;

public class FireCaptainShotgun : GenericBulletBaseState
{
	public static float tightSoundSwitchThreshold;

	public static string wideSoundString;

	public static string tightSoundString;

	private static int FireCaptainShotgunStateHash = Animator.StringToHash("FireCaptainShotgun");

	public override void OnEnter()
	{
		fireSoundString = ((base.characterBody.spreadBloomAngle <= tightSoundSwitchThreshold) ? tightSoundString : wideSoundString);
		base.OnEnter();
		PlayAnimation("Gesture, Additive", FireCaptainShotgunStateHash);
		PlayAnimation("Gesture, Override", FireCaptainShotgunStateHash);
	}

	protected override void ModifyBullet(BulletAttack bulletAttack)
	{
		base.ModifyBullet(bulletAttack);
		bulletAttack.falloffModel = BulletAttack.FalloffModel.DefaultBullet;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}

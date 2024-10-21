using RoR2;
using UnityEngine;

namespace EntityStates.Toolbot;

public class NailgunFinalBurst : BaseNailgunState
{
	public static int finalBurstBulletCount = 8;

	public static float burstTimeCostCoefficient = 1.2f;

	public static string burstSound;

	public static float selfForce = 1000f;

	private static int FireGrenadeLauncherStateHash = Animator.StringToHash("FireGrenadeLauncher");

	private static int FireGrenadeLauncherParamHash = Animator.StringToHash("FireGrenadeLauncher.playbackRate");

	protected override float GetBaseDuration()
	{
		return (float)finalBurstBulletCount * FireNailgun.baseRefireInterval * burstTimeCostCoefficient;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.characterBody)
		{
			base.characterBody.SetSpreadBloom(1f, canOnlyIncreaseBloom: false);
		}
		if (!base.characterBody.HasBuff(DLC2Content.Buffs.DisableAllSkills.buffIndex))
		{
			Ray ray = GetAimRay();
			TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, BaseNailgunState.maxDistance, base.gameObject);
			FireBullet(GetAimRay(), finalBurstBulletCount, BaseNailgunState.spreadPitchScale, BaseNailgunState.spreadYawScale);
			if (!base.isInDualWield)
			{
				PlayAnimation("Gesture, Additive", FireGrenadeLauncherStateHash, FireGrenadeLauncherParamHash, 0.45f / attackSpeedStat);
			}
			else
			{
				BaseToolbotPrimarySkillStateMethods.PlayGenericFireAnim(this, base.gameObject, base.skillLocator, 0.45f / attackSpeedStat);
			}
			Util.PlaySound(burstSound, base.gameObject);
			if (base.isAuthority)
			{
				float num = selfForce * (base.characterMotor.isGrounded ? 0.5f : 1f) * base.characterMotor.mass;
				base.characterMotor.ApplyForce(ray.direction * (0f - num));
			}
			Util.PlaySound(BaseNailgunState.fireSoundString, base.gameObject);
			Util.PlaySound(BaseNailgunState.fireSoundString, base.gameObject);
			Util.PlaySound(BaseNailgunState.fireSoundString, base.gameObject);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}

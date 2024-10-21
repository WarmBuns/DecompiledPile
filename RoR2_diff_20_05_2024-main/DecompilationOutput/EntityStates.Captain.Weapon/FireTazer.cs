using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Captain.Weapon;

public class FireTazer : BaseState
{
	public static GameObject projectilePrefab;

	public static GameObject muzzleflashEffectPrefab;

	public static GameObject chargeEffectPrefab;

	public static float baseDelay = 0.1f;

	public static float baseDuration = 2f;

	public static float baseDurationUntilPriorityLowers = 1f;

	public static float damageCoefficient = 1.2f;

	public static float force = 20f;

	public static string enterSoundString;

	public static string attackString;

	public static float recoilAmplitude;

	public static float bloom;

	public static string targetMuzzle;

	private float duration;

	private float delay;

	private float durationUntilPriorityLowers;

	private static int FireTazerStateHash = Animator.StringToHash("FireTazer");

	private static int FireTazerParamHash = Animator.StringToHash("FireTazer.playbackRate");

	private bool hasFired;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		durationUntilPriorityLowers = baseDurationUntilPriorityLowers / attackSpeedStat;
		delay = baseDelay / attackSpeedStat;
		StartAimMode(duration + 2f);
		if ((bool)chargeEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(chargeEffectPrefab, base.gameObject, targetMuzzle, transmit: false);
		}
		Util.PlayAttackSpeedSound(enterSoundString, base.gameObject, attackSpeedStat);
		PlayAnimation("Gesture, Additive", FireTazerStateHash, FireTazerParamHash, duration);
		PlayAnimation("Gesture, Override", FireTazerStateHash, FireTazerParamHash, duration);
	}

	private void Fire()
	{
		hasFired = true;
		Util.PlaySound(attackString, base.gameObject);
		AddRecoil(-1f * recoilAmplitude, -1.5f * recoilAmplitude, -0.25f * recoilAmplitude, 0.25f * recoilAmplitude);
		base.characterBody.AddSpreadBloom(bloom);
		Ray ray = GetAimRay();
		if ((bool)muzzleflashEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, targetMuzzle, transmit: false);
		}
		if (base.isAuthority)
		{
			TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, projectilePrefab, base.gameObject);
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.projectilePrefab = projectilePrefab;
			fireProjectileInfo.position = ray.origin;
			fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(ray.direction);
			fireProjectileInfo.owner = base.gameObject;
			fireProjectileInfo.damage = damageStat * damageCoefficient;
			fireProjectileInfo.force = force;
			fireProjectileInfo.crit = Util.CheckRoll(critStat, base.characterBody.master);
			ProjectileManager.instance.FireProjectile(fireProjectileInfo);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= delay && !hasFired)
		{
			Fire();
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		if (!(base.fixedAge > durationUntilPriorityLowers))
		{
			return InterruptPriority.PrioritySkill;
		}
		return InterruptPriority.Any;
	}
}

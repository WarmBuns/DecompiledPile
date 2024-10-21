using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Treebot.Weapon;

public class FireSyringe : BaseState
{
	public static GameObject projectilePrefab;

	public static GameObject finalProjectilePrefab;

	public static GameObject muzzleflashEffectPrefab;

	public static int projectileCount = 3;

	public static float totalYawSpread = 5f;

	public static float baseDuration = 2f;

	public static float baseFireDuration = 0.2f;

	public static float damageCoefficient = 1.2f;

	public static float force = 20f;

	public static string attackSound;

	public static string finalAttackSound;

	public static string muzzleName;

	private float duration;

	private float fireDuration;

	private int projectilesFired;

	private static int FireSyringeStateHash = Animator.StringToHash("FireSyringe");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		fireDuration = baseFireDuration / attackSpeedStat;
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		int num = Mathf.FloorToInt(base.fixedAge / fireDuration * (float)projectileCount);
		if (projectilesFired <= num && projectilesFired < projectileCount)
		{
			GameObject prefab = projectilePrefab;
			string soundString = attackSound;
			if (projectilesFired == projectileCount - 1)
			{
				prefab = finalProjectilePrefab;
				soundString = finalAttackSound;
			}
			PlayAnimation("Gesture, Additive", FireSyringeStateHash);
			Util.PlaySound(soundString, base.gameObject);
			base.characterBody.SetAimTimer(3f);
			if ((bool)muzzleflashEffectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleName, transmit: false);
			}
			if (base.isAuthority)
			{
				Ray ray = GetAimRay();
				TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, projectilePrefab, base.gameObject);
				float bonusYaw = (float)Mathf.FloorToInt((float)projectilesFired - (float)(projectileCount - 1) / 2f) / (float)(projectileCount - 1) * totalYawSpread;
				Vector3 forward = Util.ApplySpread(ray.direction, 0f, 0f, 1f, 1f, bonusYaw);
				ProjectileManager.instance.FireProjectile(prefab, ray.origin, Util.QuaternionSafeLookRotation(forward), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
			}
			projectilesFired++;
		}
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

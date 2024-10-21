using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.UrchinTurret.Weapon;

public class MinigunFire : MinigunState
{
	public static GameObject muzzleVfxPrefab;

	public static GameObject projectilePrefab;

	public static float baseFireInterval;

	public static float baseDamagePerSecondCoefficient;

	public static float bulletMinSpread;

	public static float bulletMaxSpread;

	public static string fireSound;

	public static string startSound;

	public static string endSound;

	private float fireTimer;

	private Transform muzzleVfxTransform;

	private float baseFireRate;

	private float baseBulletsPerSecond;

	private Run.FixedTimeStamp critEndTime;

	private Run.FixedTimeStamp lastCritCheck;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)muzzleTransform && (bool)muzzleVfxPrefab)
		{
			muzzleVfxTransform = Object.Instantiate(muzzleVfxPrefab, muzzleTransform).transform;
		}
		baseFireRate = 1f / baseFireInterval;
		baseBulletsPerSecond = baseFireRate;
		critEndTime = Run.FixedTimeStamp.negativeInfinity;
		lastCritCheck = Run.FixedTimeStamp.negativeInfinity;
		Util.PlaySound(startSound, base.gameObject);
		PlayCrossfade("Gesture, Additive", "ShootLoop", 0.2f);
	}

	private void UpdateCrits()
	{
		if (lastCritCheck.timeSince >= 1f)
		{
			lastCritCheck = Run.FixedTimeStamp.now;
			if (RollCrit())
			{
				critEndTime = Run.FixedTimeStamp.now + 2f;
			}
		}
	}

	public override void OnExit()
	{
		Util.PlaySound(endSound, base.gameObject);
		if ((bool)muzzleVfxTransform)
		{
			EntityState.Destroy(muzzleVfxTransform.gameObject);
			muzzleVfxTransform = null;
		}
		PlayCrossfade("Gesture, Additive", "BufferEmpty", 0.2f);
		base.OnExit();
	}

	private void OnFireShared()
	{
		Util.PlaySound(fireSound, base.gameObject);
		if (base.isAuthority)
		{
			OnFireAuthority();
		}
	}

	private void OnFireAuthority()
	{
		UpdateCrits();
		bool crit = !critEndTime.hasPassed;
		float damage = baseDamagePerSecondCoefficient / baseBulletsPerSecond * damageStat;
		Ray aimRay = GetAimRay();
		Vector3 forward = Util.ApplySpread(aimRay.direction, bulletMinSpread, bulletMaxSpread, 1f, 1f);
		ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(forward), base.gameObject, damage, 0f, crit);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		fireTimer -= GetDeltaTime();
		if (fireTimer <= 0f)
		{
			float num = baseFireInterval / attackSpeedStat;
			fireTimer += num;
			OnFireShared();
		}
		if (base.isAuthority && !base.skillButtonState.down)
		{
			outer.SetNextState(new MinigunSpinDown());
		}
	}
}

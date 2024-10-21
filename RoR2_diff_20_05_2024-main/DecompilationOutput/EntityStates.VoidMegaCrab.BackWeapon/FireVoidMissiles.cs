using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.VoidMegaCrab.BackWeapon;

public class FireVoidMissiles : BaseSkillState
{
	public static float baseDuration;

	public static string leftMuzzleString;

	public static string rightMuzzleString;

	public static GameObject muzzleEffectPrefab;

	public static GameObject projectilePrefab;

	public static int totalMissileWaveCount;

	public static float baseDurationBetweenMissiles;

	public static float damageCoefficient = 1.2f;

	public static float force = 20f;

	public static string enterSoundString;

	public static string animationLayerName = "Gesture, Additive";

	public static string animationStateName = "FireCrabCannon";

	public static string animationPlaybackRateParam = "FireCrabCannon.playbackRate";

	private float duration;

	private float durationBetweenMissiles;

	private float missileTimer;

	private int missileWaveCount;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		durationBetweenMissiles = baseDurationBetweenMissiles / attackSpeedStat;
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	private void FireMissile()
	{
		if ((bool)muzzleEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, leftMuzzleString, transmit: false);
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, rightMuzzleString, transmit: false);
		}
		if (base.isAuthority)
		{
			Transform transform = FindModelChild(leftMuzzleString);
			Transform transform2 = FindModelChild(rightMuzzleString);
			Ray ray = GetAimRay();
			if (transform != null)
			{
				ray = new Ray(transform.position, transform.forward);
			}
			ProjectileManager.instance.FireProjectile(projectilePrefab, ray.origin, Util.QuaternionSafeLookRotation(ray.direction), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
			if (transform2 != null)
			{
				ray = new Ray(transform2.position, transform2.forward);
			}
			ProjectileManager.instance.FireProjectile(projectilePrefab, ray.origin, Util.QuaternionSafeLookRotation(ray.direction), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		missileTimer -= GetDeltaTime();
		if (missileWaveCount < totalMissileWaveCount && missileTimer <= 0f)
		{
			missileWaveCount++;
			missileTimer += durationBetweenMissiles;
			FireMissile();
		}
		if (base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}
}

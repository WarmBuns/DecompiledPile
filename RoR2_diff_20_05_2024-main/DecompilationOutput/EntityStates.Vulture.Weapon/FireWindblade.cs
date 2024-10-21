using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Vulture.Weapon;

public class FireWindblade : BaseSkillState
{
	public static float baseDuration;

	public static string muzzleString;

	public static GameObject muzzleEffectPrefab;

	public static GameObject projectilePrefab;

	public static float damageCoefficient = 1.2f;

	public static float force = 20f;

	public static string soundString;

	private float duration;

	private static int FireWindbladeStateHash = Animator.StringToHash("FireWindblade");

	private static int FireWindbladeParamHash = Animator.StringToHash("FireWindblade.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("Gesture, Additive", FireWindbladeStateHash, FireWindbladeParamHash, duration);
		Util.PlaySound(soundString, base.gameObject);
		if ((bool)muzzleEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, muzzleString, transmit: false);
		}
		Ray aimRay = GetAimRay();
		if (base.isAuthority)
		{
			Quaternion quaternion = Util.QuaternionSafeLookRotation(aimRay.direction);
			Quaternion quaternion2 = Quaternion.AngleAxis(Random.Range(0f, 360f), aimRay.direction);
			ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, quaternion2 * quaternion, base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}
}

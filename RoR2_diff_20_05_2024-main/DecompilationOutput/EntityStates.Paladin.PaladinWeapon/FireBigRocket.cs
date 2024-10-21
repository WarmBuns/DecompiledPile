using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Paladin.PaladinWeapon;

public class FireBigRocket : BaseState
{
	public static GameObject projectilePrefab;

	public static GameObject effectPrefab;

	public static string soundEffectString;

	public static float damageCoefficient;

	public static float force;

	public static float baseDuration = 2f;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(soundEffectString, base.gameObject);
		duration = baseDuration / attackSpeedStat;
		base.characterBody.AddSpreadBloom(1f);
		Ray aimRay = GetAimRay();
		StartAimMode(aimRay);
		string muzzleName = "MuzzleCenter";
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, muzzleName, transmit: false);
		}
		if (base.isAuthority)
		{
			ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damageStat * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}

	public override void OnExit()
	{
		base.OnExit();
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

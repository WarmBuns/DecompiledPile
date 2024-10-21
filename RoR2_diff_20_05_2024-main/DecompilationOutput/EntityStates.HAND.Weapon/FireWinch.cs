using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.HAND.Weapon;

public class FireWinch : BaseState
{
	public static GameObject projectilePrefab;

	public static GameObject effectPrefab;

	public static float duration = 2f;

	public static float baseDuration = 2f;

	public static float damageCoefficient = 1.2f;

	public static float force = 20f;

	public override void OnEnter()
	{
		base.OnEnter();
		Ray aimRay = GetAimRay();
		StartAimMode(aimRay);
		string muzzleName = "WinchHole";
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, muzzleName, transmit: false);
		}
		if (base.isAuthority)
		{
			ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration / attackSpeedStat && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}

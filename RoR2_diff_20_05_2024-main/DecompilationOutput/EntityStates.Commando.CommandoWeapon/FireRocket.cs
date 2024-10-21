using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Commando.CommandoWeapon;

public class FireRocket : BaseState
{
	public static GameObject projectilePrefab;

	public static GameObject effectPrefab;

	public static float damageCoefficient;

	public static float force;

	public static float baseDuration = 2f;

	private float duration;

	public int bulletCountCurrent = 1;

	private static int FireFMJStateHash = Animator.StringToHash("FireFMJ");

	private static int FireFMJParamHash = Animator.StringToHash("FireFMJ.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Ray aimRay = GetAimRay();
		StartAimMode(aimRay);
		string muzzleName = "MuzzleCenter";
		PlayAnimation("Gesture", FireFMJStateHash, FireFMJParamHash, duration);
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

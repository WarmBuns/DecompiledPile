using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Engi.EngiWeapon;

public class FireMines : BaseState
{
	public static GameObject effectPrefab;

	public static GameObject hitEffectPrefab;

	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public float force;

	public static float baseDuration = 2f;

	public static string throwMineSoundString;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(throwMineSoundString, base.gameObject);
		duration = baseDuration / attackSpeedStat;
		Ray aimRay = GetAimRay();
		StartAimMode(aimRay);
		if ((bool)GetModelAnimator())
		{
			float num = duration * 0.3f;
			PlayCrossfade("Gesture, Additive", "FireMineRight", "FireMine.playbackRate", duration + num, 0.05f);
		}
		string muzzleName = "MuzzleCenter";
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
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

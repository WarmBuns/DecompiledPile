using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Huntress.HuntressWeapon;

public class FireArrowOld : BaseState
{
	public static GameObject projectilePrefab;

	public static GameObject effectPrefab;

	public static float baseDuration = 2f;

	public static float damageCoefficient = 1.2f;

	public static float force = 20f;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Ray aimRay = GetAimRay();
		StartAimMode(aimRay);
		string muzzleName = "Muzzle";
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, muzzleName, transmit: false);
		}
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			int layerIndex = modelAnimator.GetLayerIndex("Gesture");
			modelAnimator.SetFloat("FireArrow.playbackRate", attackSpeedStat);
			modelAnimator.PlayInFixedTime("FireArrow", layerIndex, 0f);
			muzzleName = "MuzzleRight";
			modelAnimator.Update(0f);
			if (base.isAuthority)
			{
				ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
			}
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

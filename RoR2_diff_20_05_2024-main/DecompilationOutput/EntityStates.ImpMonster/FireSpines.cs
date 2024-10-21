using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.ImpMonster;

public class FireSpines : BaseState
{
	public static GameObject projectilePrefab;

	public static GameObject effectPrefab;

	public static float baseDuration = 2f;

	public static float durationBetweenThrows = 0.1f;

	public static int spineCountMax = 3;

	public static float damageCoefficient = 1.2f;

	public static float force = 20f;

	private int spineCount;

	private float spineTimer;

	private float duration;

	private static int FireSpinesStateHash = Animator.StringToHash("FireSpines");

	private static int FireSpinesParamHash = Animator.StringToHash("FireSpines.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("Gesture", FireSpinesStateHash, FireSpinesParamHash, duration);
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		spineTimer += GetDeltaTime();
		if (spineTimer >= durationBetweenThrows / attackSpeedStat && spineCount < spineCountMax)
		{
			spineCount++;
			Ray aimRay = GetAimRay();
			string muzzleName = "MuzzleMouth";
			spineTimer -= durationBetweenThrows / attackSpeedStat;
			if ((bool)effectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, muzzleName, transmit: false);
			}
			if (base.isAuthority)
			{
				ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master));
			}
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

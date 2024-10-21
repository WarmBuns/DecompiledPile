using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.ChildMonster;

public class SparkBallFire : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject projectilePrefab;

	public static GameObject muzzleEffectPrefab;

	public static string fireBombSoundString;

	public static float bombDamageCoefficient;

	public static float bombForce;

	public float novaRadius;

	private float duration;

	private float stopwatch;

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("Gesture, Override", "SparkLaunch", "SparkLaunch.playbackRate", duration);
		FireBomb();
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private void FireBomb()
	{
		Ray aimRay = GetAimRay();
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				aimRay.origin = component.FindChild("MuzzleFire").transform.position;
			}
		}
		EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, "MuzzleFire", transmit: false);
		if (base.isAuthority)
		{
			ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damageStat * bombDamageCoefficient, bombForce, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}
}

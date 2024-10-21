using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.GravekeeperBoss;

public class FireHook : BaseState
{
	public static float baseDuration = 3f;

	public static string soundString;

	public static string muzzleString;

	public static float projectileDamageCoefficient;

	public static GameObject muzzleflashEffectPrefab;

	public static GameObject projectilePrefab;

	public static float spread;

	public static int projectileCount;

	public static float projectileForce;

	private float duration;

	private Animator modelAnimator;

	public override void OnEnter()
	{
		base.OnEnter();
		base.fixedAge = 0f;
		duration = baseDuration / attackSpeedStat;
		modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			PlayCrossfade("Body", "FireHook", "FireHook.playbackRate", duration, 0.03f);
		}
		ChildLocator component = modelAnimator.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			component.FindChild(muzzleString);
		}
		Util.PlayAttackSpeedSound(soundString, base.gameObject, attackSpeedStat);
		EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: false);
		Ray aimRay = GetAimRay();
		if (NetworkServer.active)
		{
			FireSingleHook(aimRay, 0f, 0f);
			for (int i = 0; i < projectileCount; i++)
			{
				float bonusPitch = Random.Range(0f - spread, spread) / 2f;
				float bonusYaw = Random.Range(0f - spread, spread) / 2f;
				FireSingleHook(aimRay, bonusPitch, bonusYaw);
			}
		}
	}

	private void FireSingleHook(Ray aimRay, float bonusPitch, float bonusYaw)
	{
		Vector3 forward = Util.ApplySpread(aimRay.direction, 0f, 0f, 1f, 1f, bonusYaw, bonusPitch);
		ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(forward), base.gameObject, damageStat * projectileDamageCoefficient, projectileForce, Util.CheckRoll(critStat, base.characterBody.master));
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

using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.RoboBallBoss.Weapon;

public class FireEyeBlast : BaseState
{
	[SerializeField]
	public GameObject projectilePrefab;

	public static GameObject muzzleflashEffectPrefab;

	[SerializeField]
	public int projectileCount = 3;

	[SerializeField]
	public float totalYawSpread = 5f;

	[SerializeField]
	public float baseDuration = 2f;

	[SerializeField]
	public float baseFireDuration = 0.2f;

	[SerializeField]
	public float damageCoefficient = 1.2f;

	[SerializeField]
	public float projectileSpeed;

	public static float force = 20f;

	public static float selfForce;

	public static string attackString;

	public static string muzzleString;

	private float duration;

	private float fireDuration;

	private int projectilesFired;

	private bool projectileSpreadIsYaw;

	private static int FireEyeBlastStateHash = Animator.StringToHash("FireEyeBlast");

	private static int FireEyeBlastParamHash = Animator.StringToHash("FireEyeBlast.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		fireDuration = baseFireDuration / attackSpeedStat;
		PlayAnimation("Gesture, Additive", FireEyeBlastStateHash, FireEyeBlastParamHash, duration);
		Util.PlaySound(attackString, base.gameObject);
		if (base.isAuthority)
		{
			base.healthComponent.TakeDamageForce(GetAimRay().direction * selfForce);
		}
		if (Random.value <= 0.5f)
		{
			projectileSpreadIsYaw = true;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			int num = Mathf.FloorToInt(base.fixedAge / fireDuration * (float)projectileCount);
			if (projectilesFired <= num && projectilesFired < projectileCount)
			{
				if ((bool)muzzleflashEffectPrefab)
				{
					EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: false);
				}
				Ray aimRay = GetAimRay();
				float speedOverride = projectileSpeed;
				int num2 = Mathf.FloorToInt((float)projectilesFired - (float)(projectileCount - 1) / 2f);
				float bonusYaw = 0f;
				float bonusPitch = 0f;
				if (projectileSpreadIsYaw)
				{
					bonusYaw = (float)num2 / (float)(projectileCount - 1) * totalYawSpread;
				}
				else
				{
					bonusPitch = (float)num2 / (float)(projectileCount - 1) * totalYawSpread;
				}
				Vector3 forward = Util.ApplySpread(aimRay.direction, 0f, 0f, 1f, 1f, bonusYaw, bonusPitch);
				ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(forward), base.gameObject, damageStat * damageCoefficient, force, Util.CheckRoll(critStat, base.characterBody.master), DamageColorIndex.Default, null, speedOverride);
				projectilesFired++;
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

using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Bandit2.Weapon;

public class Bandit2FireShiv : BaseSkillState
{
	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public float force;

	[SerializeField]
	public int maxShivCount;

	public static float baseDelayBetweenShivs;

	public static float shortHopVelocity;

	public static GameObject muzzleEffectPrefab;

	public static string muzzleString;

	private float duration;

	private float delayBetweenShivs;

	private float countdownSinceLastShiv;

	private int shivCount;

	private static int SlashBladeStateHash = Animator.StringToHash("SlashBlade");

	private static int SlashBladeParamHash = Animator.StringToHash("SlashBlade.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		delayBetweenShivs = baseDelayBetweenShivs / attackSpeedStat;
		PlayAnimation("Gesture, Additive", SlashBladeStateHash, SlashBladeParamHash, duration);
		StartAimMode();
		if ((bool)base.characterMotor)
		{
			base.characterMotor.velocity = new Vector3(base.characterMotor.velocity.x, Mathf.Max(base.characterMotor.velocity.y, shortHopVelocity), base.characterMotor.velocity.z);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		countdownSinceLastShiv -= GetDeltaTime();
		if (shivCount < maxShivCount && countdownSinceLastShiv <= 0f)
		{
			shivCount++;
			countdownSinceLastShiv += delayBetweenShivs;
			FireShiv();
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	private void FireShiv()
	{
		if ((bool)muzzleEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, muzzleString, transmit: false);
		}
		if (base.isAuthority)
		{
			Ray ray = GetAimRay();
			if (projectilePrefab != null)
			{
				TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, projectilePrefab, base.gameObject);
				FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
				fireProjectileInfo.projectilePrefab = projectilePrefab;
				fireProjectileInfo.position = ray.origin;
				fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(ray.direction);
				fireProjectileInfo.owner = base.gameObject;
				fireProjectileInfo.damage = damageStat * damageCoefficient;
				fireProjectileInfo.force = force;
				fireProjectileInfo.crit = RollCrit();
				fireProjectileInfo.damageTypeOverride = DamageType.SuperBleedOnCrit;
				FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
				ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

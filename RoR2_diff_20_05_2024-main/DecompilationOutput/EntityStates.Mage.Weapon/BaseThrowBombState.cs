using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Mage.Weapon;

public abstract class BaseThrowBombState : BaseState
{
	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public GameObject muzzleflashEffectPrefab;

	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public float minDamageCoefficient;

	[SerializeField]
	public float maxDamageCoefficient;

	[SerializeField]
	public float force;

	[SerializeField]
	public float selfForce;

	protected float duration;

	public float charge;

	private static int FireNovaBombStateHash = Animator.StringToHash("FireNovaBomb");

	private static int FireNovaBombParamHash = Animator.StringToHash("FireNovaBomb.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayThrowAnimation();
		if ((bool)muzzleflashEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, "MuzzleLeft", transmit: false);
			EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, "MuzzleRight", transmit: false);
		}
		Fire();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	private void Fire()
	{
		if (base.isAuthority)
		{
			Ray ray = GetAimRay();
			if (projectilePrefab != null)
			{
				float num = Util.Remap(charge, 0f, 1f, minDamageCoefficient, maxDamageCoefficient);
				float num2 = charge * force;
				FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
				fireProjectileInfo.projectilePrefab = projectilePrefab;
				fireProjectileInfo.position = ray.origin;
				fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(ray.direction);
				fireProjectileInfo.owner = base.gameObject;
				fireProjectileInfo.damage = damageStat * num;
				fireProjectileInfo.force = num2;
				fireProjectileInfo.crit = RollCrit();
				FireProjectileInfo projectileInfo = fireProjectileInfo;
				ModifyProjectile(ref projectileInfo);
				TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, ref projectileInfo);
				ProjectileManager.instance.FireProjectile(projectileInfo);
			}
			if ((bool)base.characterMotor)
			{
				base.characterMotor.ApplyForce(ray.direction * ((0f - selfForce) * charge));
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	protected virtual void PlayThrowAnimation()
	{
		PlayAnimation("Gesture, Additive", FireNovaBombStateHash, FireNovaBombParamHash, duration);
	}

	protected virtual void ModifyProjectile(ref FireProjectileInfo projectileInfo)
	{
	}
}

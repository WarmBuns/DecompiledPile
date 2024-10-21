using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Railgunner.Weapon;

public class FirePistol : BaseState, IBaseWeaponState
{
	[SerializeField]
	public float baseDuration;

	[Header("Projectile")]
	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public float force;

	[SerializeField]
	public float spreadBloomValue;

	[SerializeField]
	public float recoilYMin;

	[SerializeField]
	public float recoilXMin;

	[SerializeField]
	public float recoilYMax;

	[SerializeField]
	public float recoilXMax;

	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public float selfKnockbackForce;

	[SerializeField]
	public float baseInaccuracyDegrees;

	[Header("Effects")]
	[SerializeField]
	public string muzzleName;

	[SerializeField]
	public string fireSoundString;

	[SerializeField]
	public GameObject muzzleFlashPrefab;

	[Header("Animation")]
	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	protected float duration;

	protected virtual void FireBullet(Ray aimRay)
	{
		StartAimMode(aimRay);
		Util.PlaySound(fireSoundString, base.gameObject);
		EffectManager.SimpleMuzzleFlash(muzzleFlashPrefab, base.gameObject, muzzleName, transmit: false);
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		AddRecoil(recoilYMin, recoilYMax, recoilXMin, recoilXMax);
		if (base.isAuthority)
		{
			float num = 0f;
			if ((bool)base.characterBody)
			{
				num = base.characterBody.spreadBloomAngle;
			}
			Quaternion quaternion = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward);
			Quaternion quaternion2 = Quaternion.AngleAxis(Random.Range(0f, baseInaccuracyDegrees + num), Vector3.left);
			Quaternion rotation = Util.QuaternionSafeLookRotation(aimRay.direction, Vector3.up) * quaternion * quaternion2;
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.projectilePrefab = projectilePrefab;
			fireProjectileInfo.position = aimRay.origin;
			fireProjectileInfo.rotation = rotation;
			fireProjectileInfo.owner = base.gameObject;
			fireProjectileInfo.damage = damageStat * damageCoefficient;
			fireProjectileInfo.crit = RollCrit();
			fireProjectileInfo.force = force;
			fireProjectileInfo.procChainMask = default(ProcChainMask);
			fireProjectileInfo.damageColorIndex = DamageColorIndex.Default;
			FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
			ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
			base.characterBody.characterMotor.ApplyForce((0f - selfKnockbackForce) * aimRay.direction);
		}
		base.characterBody.AddSpreadBloom(spreadBloomValue);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		FireBullet(GetAimRay());
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}

	public bool CanScope()
	{
		return true;
	}
}

using RoR2;
using UnityEngine;

namespace EntityStates;

public abstract class GenericBulletBaseState : BaseState
{
	[SerializeField]
	public float baseDuration = 0.1f;

	[SerializeField]
	public int bulletCount = 1;

	[SerializeField]
	public float maxDistance = 50f;

	[SerializeField]
	public float bulletRadius;

	[SerializeField]
	public bool useSmartCollision;

	[SerializeField]
	public float damageCoefficient = 0.1f;

	[SerializeField]
	public float procCoefficient = 1f;

	[SerializeField]
	public float force = 100f;

	[SerializeField]
	public float minSpread;

	[SerializeField]
	public float maxSpread;

	[SerializeField]
	public float spreadPitchScale = 0.5f;

	[SerializeField]
	public float spreadYawScale = 1f;

	[SerializeField]
	public float spreadBloomValue = 0.2f;

	[SerializeField]
	public float recoilAmplitudeY;

	[SerializeField]
	public float recoilAmplitudeX;

	[SerializeField]
	public string muzzleName;

	[SerializeField]
	public string fireSoundString;

	[SerializeField]
	public GameObject muzzleFlashPrefab;

	[SerializeField]
	public GameObject tracerEffectPrefab;

	[SerializeField]
	public GameObject hitEffectPrefab;

	protected float duration;

	protected Transform muzzleTransform;

	protected BulletAttack GenerateBulletAttack(Ray aimRay)
	{
		float num = 0f;
		if ((bool)base.characterBody)
		{
			num = base.characterBody.spreadBloomAngle;
		}
		return new BulletAttack
		{
			aimVector = aimRay.direction,
			origin = aimRay.origin,
			owner = base.gameObject,
			weapon = null,
			bulletCount = (uint)bulletCount,
			damage = damageStat * damageCoefficient,
			damageColorIndex = DamageColorIndex.Default,
			damageType = DamageType.Generic,
			falloffModel = BulletAttack.FalloffModel.Buckshot,
			force = force,
			HitEffectNormal = false,
			procChainMask = default(ProcChainMask),
			procCoefficient = procCoefficient,
			maxDistance = maxDistance,
			radius = bulletRadius,
			isCrit = RollCrit(),
			muzzleName = muzzleName,
			minSpread = minSpread,
			maxSpread = maxSpread + num,
			hitEffectPrefab = hitEffectPrefab,
			smartCollision = useSmartCollision,
			sniper = false,
			spreadPitchScale = spreadPitchScale,
			spreadYawScale = spreadYawScale,
			tracerEffectPrefab = tracerEffectPrefab
		};
	}

	protected virtual void PlayFireAnimation()
	{
	}

	protected virtual void DoFireEffects()
	{
		Util.PlaySound(fireSoundString, base.gameObject);
		if ((bool)muzzleTransform)
		{
			EffectManager.SimpleMuzzleFlash(muzzleFlashPrefab, base.gameObject, muzzleName, transmit: false);
		}
	}

	protected virtual void ModifyBullet(BulletAttack bulletAttack)
	{
	}

	protected virtual void FireBullet(Ray aimRay)
	{
		StartAimMode(aimRay, 3f);
		DoFireEffects();
		PlayFireAnimation();
		AddRecoil(-1f * recoilAmplitudeY, -1.5f * recoilAmplitudeY, -1f * recoilAmplitudeX, 1f * recoilAmplitudeX);
		if (base.isAuthority)
		{
			BulletAttack bulletAttack = GenerateBulletAttack(aimRay);
			ModifyBullet(bulletAttack);
			bulletAttack.Fire();
			OnFireBulletAuthority(aimRay);
		}
	}

	protected virtual void OnFireBulletAuthority(Ray aimRay)
	{
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		muzzleTransform = FindModelChild(muzzleName);
		FireBullet(GetAimRay());
		base.characterBody.AddSpreadBloom(spreadBloomValue);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(InstantiateNextState());
		}
	}

	protected virtual EntityState InstantiateNextState()
	{
		return EntityStateCatalog.InstantiateState(ref outer.mainStateType);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}

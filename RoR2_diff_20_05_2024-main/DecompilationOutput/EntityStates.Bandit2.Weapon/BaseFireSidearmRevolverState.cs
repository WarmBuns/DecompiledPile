using RoR2;
using UnityEngine;

namespace EntityStates.Bandit2.Weapon;

public abstract class BaseFireSidearmRevolverState : BaseSidearmState
{
	[SerializeField]
	public GameObject effectPrefab;

	[SerializeField]
	public GameObject hitEffectPrefab;

	[SerializeField]
	public GameObject tracerEffectPrefab;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public float force;

	[SerializeField]
	public float minSpread;

	[SerializeField]
	public float maxSpread;

	[SerializeField]
	public string attackSoundString;

	[SerializeField]
	public float recoilAmplitude;

	[SerializeField]
	public float bulletRadius;

	[SerializeField]
	public float trajectoryAimAssistMultiplier = 0.75f;

	private static int FireSideWeaponStateHash = Animator.StringToHash("FireSideWeapon");

	private static int FireSideWeaponParamHash = Animator.StringToHash("FireSideWeapon.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		AddRecoil(-3f * recoilAmplitude, -4f * recoilAmplitude, -0.5f * recoilAmplitude, 0.5f * recoilAmplitude);
		Ray aimRay = GetAimRay();
		StartAimMode(aimRay);
		string muzzleName = "MuzzlePistol";
		Util.PlaySound(attackSoundString, base.gameObject);
		PlayAnimation("Gesture, Additive", FireSideWeaponStateHash, FireSideWeaponParamHash, duration);
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, muzzleName, transmit: false);
		}
		if (base.isAuthority)
		{
			BulletAttack bulletAttack = new BulletAttack();
			bulletAttack.owner = base.gameObject;
			bulletAttack.weapon = base.gameObject;
			bulletAttack.origin = aimRay.origin;
			bulletAttack.aimVector = aimRay.direction;
			bulletAttack.minSpread = minSpread;
			bulletAttack.maxSpread = maxSpread;
			bulletAttack.bulletCount = 1u;
			bulletAttack.damage = damageCoefficient * damageStat;
			bulletAttack.force = force;
			bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
			bulletAttack.tracerEffectPrefab = tracerEffectPrefab;
			bulletAttack.muzzleName = muzzleName;
			bulletAttack.hitEffectPrefab = hitEffectPrefab;
			bulletAttack.isCrit = RollCrit();
			bulletAttack.HitEffectNormal = false;
			bulletAttack.radius = bulletRadius;
			bulletAttack.damageType |= (DamageTypeCombo)DamageType.BonusToLowHealth;
			bulletAttack.smartCollision = true;
			bulletAttack.trajectoryAimAssistMultiplier = trajectoryAimAssistMultiplier;
			ModifyBullet(bulletAttack);
			bulletAttack.Fire();
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
			outer.SetNextState(new ExitSidearmRevolver());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Any;
	}

	protected virtual void ModifyBullet(BulletAttack bulletAttack)
	{
	}
}

using RoR2;
using UnityEngine;

namespace EntityStates.VoidSurvivor.Weapon;

public class FireHandBeam : BaseState
{
	[SerializeField]
	public GameObject muzzleflashEffectPrefab;

	[SerializeField]
	public GameObject hitEffectPrefab;

	[SerializeField]
	public GameObject tracerEffectPrefab;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public float maxDistance;

	[SerializeField]
	public float force;

	[SerializeField]
	public int bulletCount;

	[SerializeField]
	public float bulletRadius;

	[SerializeField]
	public float baseDuration = 2f;

	[SerializeField]
	public string attackSoundString;

	[SerializeField]
	public float recoilAmplitude;

	[SerializeField]
	public float spreadBloomValue = 0.3f;

	[SerializeField]
	public float maxSpread;

	[SerializeField]
	public string muzzle;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public float trajectoryAimAssistMultiplier = 0.75f;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Ray aimRay = GetAimRay();
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		AddRecoil(-1f * recoilAmplitude, -2f * recoilAmplitude, -0.5f * recoilAmplitude, 0.5f * recoilAmplitude);
		StartAimMode(aimRay);
		Util.PlaySound(attackSoundString, base.gameObject);
		if ((bool)muzzleflashEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzle, transmit: false);
		}
		if (base.isAuthority)
		{
			BulletAttack bulletAttack = new BulletAttack();
			bulletAttack.owner = base.gameObject;
			bulletAttack.weapon = base.gameObject;
			bulletAttack.origin = aimRay.origin;
			bulletAttack.aimVector = aimRay.direction;
			bulletAttack.muzzleName = muzzle;
			bulletAttack.maxDistance = maxDistance;
			bulletAttack.minSpread = 0f;
			bulletAttack.maxSpread = base.characterBody.spreadBloomAngle;
			bulletAttack.radius = bulletRadius;
			bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
			bulletAttack.smartCollision = true;
			bulletAttack.damage = damageCoefficient * damageStat;
			bulletAttack.procCoefficient = 1f / (float)bulletCount;
			bulletAttack.force = force;
			bulletAttack.isCrit = Util.CheckRoll(critStat, base.characterBody.master);
			bulletAttack.damageType = DamageType.SlowOnHit;
			bulletAttack.tracerEffectPrefab = tracerEffectPrefab;
			bulletAttack.hitEffectPrefab = hitEffectPrefab;
			bulletAttack.trajectoryAimAssistMultiplier = trajectoryAimAssistMultiplier;
			bulletAttack.Fire();
		}
		base.characterBody.AddSpreadBloom(spreadBloomValue);
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

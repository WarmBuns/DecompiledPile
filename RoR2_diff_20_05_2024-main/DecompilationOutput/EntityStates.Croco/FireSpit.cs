using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Croco;

public class FireSpit : BaseState
{
	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public GameObject effectPrefab;

	[SerializeField]
	public float baseDuration = 2f;

	[SerializeField]
	public float damageCoefficient = 1.2f;

	[SerializeField]
	public float force = 20f;

	[SerializeField]
	public string attackString;

	[SerializeField]
	public float recoilAmplitude;

	[SerializeField]
	public float bloom;

	private float duration;

	private CrocoDamageTypeController crocoDamageTypeController;

	private static int FireSpitStateHash = Animator.StringToHash("FireSpit");

	private static int FireSpitParamHash = Animator.StringToHash("FireSpit.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		crocoDamageTypeController = GetComponent<CrocoDamageTypeController>();
		Ray ray = GetAimRay();
		TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, projectilePrefab, base.gameObject);
		duration = baseDuration / attackSpeedStat;
		StartAimMode(duration + 2f);
		PlayAnimation("Gesture, Mouth", FireSpitStateHash, FireSpitParamHash, duration);
		Util.PlaySound(attackString, base.gameObject);
		AddRecoil(-1f * recoilAmplitude, -1.5f * recoilAmplitude, -0.25f * recoilAmplitude, 0.25f * recoilAmplitude);
		base.characterBody.AddSpreadBloom(bloom);
		string muzzleName = "MouthMuzzle";
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, muzzleName, transmit: false);
		}
		if (base.isAuthority)
		{
			DamageTypeCombo value = (crocoDamageTypeController ? crocoDamageTypeController.GetDamageType() : DamageTypeCombo.Generic);
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.projectilePrefab = projectilePrefab;
			fireProjectileInfo.position = ray.origin;
			fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(ray.direction);
			fireProjectileInfo.owner = base.gameObject;
			fireProjectileInfo.damage = damageStat * damageCoefficient;
			fireProjectileInfo.damageTypeOverride = value;
			fireProjectileInfo.force = force;
			fireProjectileInfo.crit = Util.CheckRoll(critStat, base.characterBody.master);
			ProjectileManager.instance.FireProjectile(fireProjectileInfo);
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
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}

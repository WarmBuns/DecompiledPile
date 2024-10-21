using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Treebot;

public class TreebotFireFruitSeed : BaseState
{
	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public string muzzleName;

	[SerializeField]
	public GameObject muzzleFlashPrefab;

	[SerializeField]
	public string animationLayerName = "Gesture, Additive";

	[SerializeField]
	public string animationStateName = "FireFlower";

	[SerializeField]
	public string playbackRateParam = "FireFlower.playbackRate";

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		EffectManager.SimpleMuzzleFlash(muzzleFlashPrefab, base.gameObject, muzzleName, transmit: false);
		duration = baseDuration / attackSpeedStat;
		Util.PlaySound(enterSoundString, base.gameObject);
		PlayAnimation(animationLayerName, animationStateName, playbackRateParam, duration);
		if (base.isAuthority)
		{
			Ray ray = GetAimRay();
			TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref ray, projectilePrefab, base.gameObject);
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.crit = RollCrit();
			fireProjectileInfo.damage = damageCoefficient * damageStat;
			fireProjectileInfo.damageColorIndex = DamageColorIndex.Default;
			fireProjectileInfo.force = 0f;
			fireProjectileInfo.owner = base.gameObject;
			fireProjectileInfo.position = ray.origin;
			fireProjectileInfo.procChainMask = default(ProcChainMask);
			fireProjectileInfo.projectilePrefab = projectilePrefab;
			fireProjectileInfo.rotation = Quaternion.LookRotation(ray.direction);
			fireProjectileInfo.useSpeedOverride = false;
			FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
			ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

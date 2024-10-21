using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;

namespace EntityStates.VoidRaidCrab.Weapon;

public class FireMissiles : BaseState
{
	[SerializeField]
	public float baseInitialDelay;

	[SerializeField]
	public float baseDelayBetweenWaves;

	[SerializeField]
	public float baseEndDelay;

	[SerializeField]
	public int numWaves;

	[SerializeField]
	public int numMissilesPerWave;

	[SerializeField]
	public string muzzleName;

	[SerializeField]
	public GameObject muzzleFlashPrefab;

	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public float force;

	[SerializeField]
	public float minSpreadDegrees;

	[SerializeField]
	public float rangeSpreadDegrees;

	[SerializeField]
	public string fireWaveSoundString;

	[SerializeField]
	public bool isSoundScaledByAttackSpeed;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public SkillDef skillDefToReplaceAtStocksEmpty;

	[SerializeField]
	public SkillDef nextSkillDef;

	private float delayBetweenWaves;

	private float duration;

	private int numWavesFired;

	private float timeUntilNextWave;

	private Transform muzzleTransform;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)nextSkillDef)
		{
			GenericSkill genericSkill = base.skillLocator.FindSkillByDef(skillDefToReplaceAtStocksEmpty);
			if ((bool)genericSkill && genericSkill.stock == 0)
			{
				genericSkill.SetBaseSkill(nextSkillDef);
			}
		}
		float num = baseInitialDelay + Mathf.Max(0f, baseDelayBetweenWaves * (float)(numWaves - 1)) + baseEndDelay;
		duration = num / attackSpeedStat;
		timeUntilNextWave = baseInitialDelay / attackSpeedStat;
		delayBetweenWaves = baseDelayBetweenWaves / attackSpeedStat;
		muzzleTransform = FindModelChild(muzzleName);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		timeUntilNextWave -= GetDeltaTime();
		while (timeUntilNextWave < 0f && numWavesFired < numWaves)
		{
			PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
			timeUntilNextWave += delayBetweenWaves;
			numWavesFired++;
			EffectManager.SimpleMuzzleFlash(muzzleFlashPrefab, base.gameObject, muzzleName, transmit: false);
			if (base.isAuthority)
			{
				Quaternion quaternion = Util.QuaternionSafeLookRotation(GetAimRay().direction);
				FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
				fireProjectileInfo.projectilePrefab = projectilePrefab;
				fireProjectileInfo.position = muzzleTransform.position;
				fireProjectileInfo.owner = base.gameObject;
				fireProjectileInfo.damage = damageStat * damageCoefficient;
				fireProjectileInfo.force = force;
				FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
				for (int i = 0; i < numMissilesPerWave; i++)
				{
					fireProjectileInfo2.rotation = quaternion * GetRandomRollPitch();
					fireProjectileInfo2.crit = Util.CheckRoll(critStat, base.characterBody.master);
					ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
				}
			}
		}
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	protected Quaternion GetRandomRollPitch()
	{
		Quaternion quaternion = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward);
		Quaternion quaternion2 = Quaternion.AngleAxis(minSpreadDegrees + Random.Range(0f, rangeSpreadDegrees), Vector3.left);
		return quaternion * quaternion2;
	}
}

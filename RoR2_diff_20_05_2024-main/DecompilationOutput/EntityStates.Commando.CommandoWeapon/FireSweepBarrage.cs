using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Commando.CommandoWeapon;

public class FireSweepBarrage : BaseState
{
	public static string enterSound;

	public static string muzzle;

	public static string fireSoundString;

	public static GameObject muzzleEffectPrefab;

	public static GameObject tracerEffectPrefab;

	public static float baseTotalDuration;

	public static float baseFiringDuration;

	public static float fieldOfView;

	public static float maxDistance;

	public static float damageCoefficient;

	public static float procCoefficient;

	public static float force;

	public static int minimumFireCount;

	public static GameObject impactEffectPrefab;

	private float totalDuration;

	private float firingDuration;

	private int totalBulletsToFire;

	private int totalBulletsFired;

	private int targetHurtboxIndex;

	private float timeBetweenBullets;

	private List<HurtBox> targetHurtboxes = new List<HurtBox>();

	private float fireTimer;

	private ChildLocator childLocator;

	private int muzzleIndex;

	private Transform muzzleTransform;

	private static int FireSweepBarrageStateHash = Animator.StringToHash("FireSweepBarrage");

	private static int FireSweepBarrageParamHash = Animator.StringToHash("FireSweepBarrage.playbackRate");

	private static int FirePistolRightStateHash = Animator.StringToHash("FirePistol, Right");

	public override void OnEnter()
	{
		base.OnEnter();
		totalDuration = baseTotalDuration / attackSpeedStat;
		firingDuration = baseFiringDuration / attackSpeedStat;
		base.characterBody.SetAimTimer(3f);
		PlayAnimation("Gesture, Additive", FireSweepBarrageStateHash, FireSweepBarrageParamHash, totalDuration);
		PlayAnimation("Gesture, Override", FireSweepBarrageStateHash, FireSweepBarrageParamHash, totalDuration);
		Util.PlaySound(enterSound, base.gameObject);
		Ray aimRay = GetAimRay();
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(GetTeam());
		bullseyeSearch.maxAngleFilter = fieldOfView * 0.5f;
		bullseyeSearch.maxDistanceFilter = maxDistance;
		bullseyeSearch.searchOrigin = aimRay.origin;
		bullseyeSearch.searchDirection = aimRay.direction;
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
		bullseyeSearch.filterByLoS = true;
		bullseyeSearch.RefreshCandidates();
		targetHurtboxes = bullseyeSearch.GetResults().Where(Util.IsValid).Distinct(default(HurtBox.EntityEqualityComparer))
			.ToList();
		totalBulletsToFire = Mathf.Max(targetHurtboxes.Count, minimumFireCount);
		timeBetweenBullets = firingDuration / (float)totalBulletsToFire;
		childLocator = GetModelTransform().GetComponent<ChildLocator>();
		muzzleIndex = childLocator.FindChildIndex(muzzle);
		muzzleTransform = childLocator.FindChild(muzzleIndex);
	}

	private void Fire()
	{
		if (totalBulletsFired >= totalBulletsToFire)
		{
			return;
		}
		if (!string.IsNullOrEmpty(muzzle))
		{
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, muzzle, transmit: false);
		}
		Util.PlaySound(fireSoundString, base.gameObject);
		PlayAnimation("Gesture Additive, Right", FirePistolRightStateHash);
		if (NetworkServer.active && targetHurtboxes.Count > 0)
		{
			DamageInfo damageInfo = new DamageInfo();
			damageInfo.damage = damageStat * damageCoefficient;
			damageInfo.attacker = base.gameObject;
			damageInfo.procCoefficient = procCoefficient;
			damageInfo.crit = Util.CheckRoll(critStat, base.characterBody.master);
			if (targetHurtboxIndex >= targetHurtboxes.Count)
			{
				targetHurtboxIndex = 0;
			}
			HurtBox hurtBox = targetHurtboxes[targetHurtboxIndex];
			if ((bool)hurtBox)
			{
				HealthComponent healthComponent = hurtBox.healthComponent;
				if ((bool)healthComponent)
				{
					targetHurtboxIndex++;
					Vector3 normalized = (hurtBox.transform.position - base.characterBody.corePosition).normalized;
					damageInfo.force = force * normalized;
					damageInfo.position = hurtBox.transform.position;
					EffectManager.SimpleImpactEffect(impactEffectPrefab, hurtBox.transform.position, normalized, transmit: true);
					healthComponent.TakeDamage(damageInfo);
					GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
				}
				if ((bool)tracerEffectPrefab && (bool)childLocator)
				{
					int childIndex = childLocator.FindChildIndex(muzzle);
					childLocator.FindChild(childIndex);
					EffectData effectData = new EffectData
					{
						origin = hurtBox.transform.position,
						start = muzzleTransform.position
					};
					effectData.SetChildLocatorTransformReference(base.gameObject, childIndex);
					EffectManager.SpawnEffect(tracerEffectPrefab, effectData, transmit: true);
				}
			}
		}
		totalBulletsFired++;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		fireTimer -= GetDeltaTime();
		if (fireTimer <= 0f)
		{
			Fire();
			fireTimer += timeBetweenBullets;
		}
		if (base.fixedAge >= totalDuration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

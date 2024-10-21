using System.Linq;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;

namespace EntityStates.GrandParentBoss;

public class GroundSwipe : BasicMeleeAttack, SteppedSkillDef.IStepSetter
{
	[SerializeField]
	public GameObject chargeEffectPrefab;

	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public float baseChargeStartDelay;

	[SerializeField]
	public float baseFireProjectileDelay;

	[SerializeField]
	public float projectileDamageCoefficient;

	[SerializeField]
	public float projectileForce;

	[SerializeField]
	public float maxBullseyeDistance;

	[SerializeField]
	public float maxBullseyeAngle;

	[SerializeField]
	public float minProjectileDistance;

	[SerializeField]
	public float maxProjectileDistance;

	[SerializeField]
	public float projectileHorizontalSpeed;

	[SerializeField]
	public string chargeSoundName;

	[SerializeField]
	public string hitBoxGroupNameLeft;

	[SerializeField]
	public string animationLayerName = "Body";

	[SerializeField]
	public string muzzleNameLeft = "SecondaryProjectileMuzzle";

	[SerializeField]
	public string muzzleNameRight = "SecondaryProjectileMuzzle";

	[SerializeField]
	public string animationStateNameLeft = "FireSecondaryProjectile";

	[SerializeField]
	public string animationStateNameRight = "FireSecondaryProjectile";

	[SerializeField]
	public string playbackRateParam = "GroundSwipe.playbackRate";

	private GameObject chargeEffectInstance;

	private bool hasFired;

	private float chargeStartDelay;

	private float fireProjectileDelay;

	private int step;

	protected ChildLocator childLocator { get; private set; }

	void SteppedSkillDef.IStepSetter.SetStep(int i)
	{
		step = i;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		hasFired = false;
		chargeStartDelay = baseChargeStartDelay / attackSpeedStat;
		fireProjectileDelay = baseFireProjectileDelay / attackSpeedStat;
		childLocator = GetModelChildLocator();
	}

	public override void OnExit()
	{
		EntityState.Destroy(chargeEffectInstance);
		base.OnExit();
	}

	protected override void PlayAnimation()
	{
		PlayCrossfade(animationLayerName, GetAnimationStateName(), playbackRateParam, duration, 1f);
	}

	public string GetMuzzleName()
	{
		if (step % 2 == 0)
		{
			return muzzleNameLeft;
		}
		return muzzleNameRight;
	}

	public string GetAnimationStateName()
	{
		if (step % 2 == 0)
		{
			return animationStateNameLeft;
		}
		return animationStateNameRight;
	}

	public override string GetHitBoxGroupName()
	{
		if (step % 2 == 0)
		{
			return hitBoxGroupNameLeft;
		}
		return hitBoxGroupName;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!chargeEffectInstance && (bool)childLocator && base.fixedAge >= chargeStartDelay)
		{
			Transform transform = childLocator.FindChild(GetMuzzleName()) ?? base.characterBody.coreTransform;
			if ((bool)transform)
			{
				if ((bool)chargeEffectPrefab)
				{
					chargeEffectInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
					chargeEffectInstance.transform.parent = transform;
					ScaleParticleSystemDuration component = chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
					ObjectScaleCurve component2 = chargeEffectInstance.GetComponent<ObjectScaleCurve>();
					if ((bool)component)
					{
						component.newDuration = duration;
					}
					if ((bool)component2)
					{
						component2.timeMax = duration;
					}
				}
				if (chargeSoundName != null)
				{
					Util.PlaySound(chargeSoundName, base.gameObject);
				}
			}
		}
		if (base.isAuthority && !hasFired && (bool)projectilePrefab && base.fixedAge >= fireProjectileDelay)
		{
			hasFired = true;
			Ray aimRay = GetAimRay();
			Ray ray = aimRay;
			Transform transform2 = childLocator.FindChild(GetMuzzleName());
			if ((bool)transform2)
			{
				ray.origin = transform2.position;
			}
			BullseyeSearch bullseyeSearch = new BullseyeSearch();
			bullseyeSearch.viewer = base.characterBody;
			bullseyeSearch.searchOrigin = base.characterBody.corePosition;
			bullseyeSearch.searchDirection = base.characterBody.corePosition;
			bullseyeSearch.maxDistanceFilter = maxBullseyeDistance;
			bullseyeSearch.maxAngleFilter = maxBullseyeAngle;
			bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(GetTeam());
			bullseyeSearch.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
			bullseyeSearch.RefreshCandidates();
			HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault();
			Vector3? vector = null;
			RaycastHit hitInfo;
			if ((bool)hurtBox)
			{
				vector = hurtBox.transform.position;
			}
			else if (Physics.Raycast(aimRay, out hitInfo, float.PositiveInfinity, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
			{
				vector = hitInfo.point;
			}
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.projectilePrefab = projectilePrefab;
			fireProjectileInfo.position = ray.origin;
			fireProjectileInfo.owner = base.gameObject;
			fireProjectileInfo.damage = damageStat * projectileDamageCoefficient;
			fireProjectileInfo.force = projectileForce;
			fireProjectileInfo.crit = RollCrit();
			FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
			if (vector.HasValue)
			{
				Vector3 vector2 = vector.Value - ray.origin;
				Vector2 vector3 = new Vector2(vector2.x, vector2.z);
				float magnitude = vector3.magnitude;
				Vector2 vector4 = vector3 / magnitude;
				magnitude = Mathf.Clamp(magnitude, minProjectileDistance, maxProjectileDistance);
				float y = Trajectory.CalculateInitialYSpeed(magnitude / projectileHorizontalSpeed, vector2.y);
				Vector3 direction = new Vector3(vector4.x * projectileHorizontalSpeed, y, vector4.y * projectileHorizontalSpeed);
				fireProjectileInfo2.speedOverride = direction.magnitude;
				ray.direction = direction;
			}
			else
			{
				fireProjectileInfo2.speedOverride = projectileHorizontalSpeed;
			}
			fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(ray.direction);
			ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
		}
	}
}

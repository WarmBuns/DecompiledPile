using System.Collections.Generic;
using System.Globalization;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Bell.BellWeapon;

public class ChargeTrioBomb : BaseState
{
	public static float basePrepDuration;

	public static float baseTimeBetweenPreps;

	public static GameObject preppedBombPrefab;

	public static float baseBarrageDuration;

	public static float baseTimeBetweenBarrages;

	public static GameObject bombProjectilePrefab;

	public static GameObject muzzleflashPrefab;

	public static float damageCoefficient;

	public static float force;

	public static float selfForce;

	private float prepDuration;

	private float timeBetweenPreps;

	private float barrageDuration;

	private float timeBetweenBarrages;

	private ChildLocator childLocator;

	private List<GameObject> preppedBombInstances = new List<GameObject>();

	private int currentBombIndex;

	private float perProjectileStopwatch;

	public override void OnEnter()
	{
		base.OnEnter();
		prepDuration = basePrepDuration / attackSpeedStat;
		timeBetweenPreps = baseTimeBetweenPreps / attackSpeedStat;
		barrageDuration = baseBarrageDuration / attackSpeedStat;
		timeBetweenBarrages = baseTimeBetweenBarrages / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			childLocator = modelTransform.GetComponent<ChildLocator>();
		}
	}

	private string FindTargetChildStringFromBombIndex()
	{
		return string.Format(CultureInfo.InvariantCulture, "ProjectilePosition{0}", currentBombIndex);
	}

	private Transform FindTargetChildTransformFromBombIndex()
	{
		string childName = FindTargetChildStringFromBombIndex();
		return childLocator.FindChild(childName);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		perProjectileStopwatch += GetDeltaTime();
		if (base.fixedAge < prepDuration)
		{
			if (perProjectileStopwatch > timeBetweenPreps && currentBombIndex < 3)
			{
				currentBombIndex++;
				perProjectileStopwatch = 0f;
				Transform transform = FindTargetChildTransformFromBombIndex();
				if ((bool)transform)
				{
					GameObject item = Object.Instantiate(preppedBombPrefab, transform);
					preppedBombInstances.Add(item);
				}
			}
		}
		else if (base.fixedAge < prepDuration + barrageDuration)
		{
			if (!(perProjectileStopwatch > timeBetweenBarrages) || currentBombIndex <= 0)
			{
				return;
			}
			perProjectileStopwatch = 0f;
			Ray aimRay = GetAimRay();
			Transform transform2 = FindTargetChildTransformFromBombIndex();
			if ((bool)transform2)
			{
				if (base.isAuthority)
				{
					float damage = damageStat * damageCoefficient;
					BaseAI component = outer.commonComponents.characterBody.master.GetComponent<BaseAI>();
					if ((object)component != null && component.shouldMissFirstOffScreenShot && component.currentEnemy.characterBody.teamComponent.teamIndex == TeamIndex.Player)
					{
						aimRay.direction = OffScreenMissHelper.ApplyRandomTrajectoryOffset(aimRay.direction);
						damage = 0f;
						if (currentBombIndex <= 1)
						{
							component.shouldMissFirstOffScreenShot = false;
						}
					}
					ProjectileManager.instance.FireProjectile(bombProjectilePrefab, transform2.position, Util.QuaternionSafeLookRotation(aimRay.direction), base.gameObject, damage, force, Util.CheckRoll(critStat, base.characterBody.master));
					Rigidbody component2 = GetComponent<Rigidbody>();
					if ((bool)component2)
					{
						component2.AddForceAtPosition((0f - selfForce) * transform2.forward, transform2.position);
					}
				}
				EffectManager.SimpleMuzzleFlash(muzzleflashPrefab, base.gameObject, FindTargetChildStringFromBombIndex(), transmit: false);
			}
			currentBombIndex--;
			EntityState.Destroy(preppedBombInstances[currentBombIndex]);
		}
		else if (base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		foreach (GameObject preppedBombInstance in preppedBombInstances)
		{
			EntityState.Destroy(preppedBombInstance);
		}
	}
}

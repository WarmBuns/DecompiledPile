using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidSurvivor.Weapon;

public class FireRepulsion : BaseState
{
	[SerializeField]
	public string sound;

	[SerializeField]
	public string muzzle;

	[SerializeField]
	public GameObject fireEffectPrefab;

	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public float fieldOfView;

	[SerializeField]
	public float backupDistance;

	[SerializeField]
	public float maxKnockbackDistance;

	[SerializeField]
	public float idealDistanceToPlaceTargets;

	[SerializeField]
	public float liftVelocity;

	[SerializeField]
	public float animationCrossfadeDuration;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackParameterName;

	[SerializeField]
	public float damageMultiplier;

	[SerializeField]
	public float maxProjectileReflectDistance;

	[SerializeField]
	public GameObject tracerEffectPrefab;

	[SerializeField]
	public float corruption;

	[SerializeField]
	public BuffDef buffDef;

	[SerializeField]
	public float buffDuration;

	public static AnimationCurve shoveSuitabilityCurve;

	private float duration;

	private Ray aimRay;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		aimRay = GetAimRay();
		PlayCrossfade(animationLayerName, animationStateName, animationPlaybackParameterName, duration, animationCrossfadeDuration);
		Util.PlaySound(sound, base.gameObject);
		Vector3 origin = aimRay.origin;
		Transform transform = FindModelChild(muzzle);
		if ((bool)transform)
		{
			origin = transform.position;
		}
		EffectManager.SpawnEffect(fireEffectPrefab, new EffectData
		{
			origin = origin,
			rotation = Quaternion.LookRotation(aimRay.direction)
		}, transmit: false);
		aimRay.origin -= aimRay.direction * backupDistance;
		if (NetworkServer.active)
		{
			PushEnemies();
			ReflectProjectiles();
			VoidSurvivorController component = GetComponent<VoidSurvivorController>();
			if ((bool)component)
			{
				component.AddCorruption(corruption);
			}
		}
	}

	private void ReflectProjectiles()
	{
		Vector3 vector = (base.characterBody ? base.characterBody.corePosition : Vector3.zero);
		TeamIndex teamIndex = (base.characterBody ? base.characterBody.teamComponent.teamIndex : TeamIndex.None);
		float num = maxProjectileReflectDistance * maxProjectileReflectDistance;
		List<ProjectileController> instancesList = InstanceTracker.GetInstancesList<ProjectileController>();
		List<ProjectileController> list = new List<ProjectileController>();
		int i = 0;
		for (int count = instancesList.Count; i < count; i++)
		{
			ProjectileController projectileController = instancesList[i];
			if (projectileController.teamFilter.teamIndex != teamIndex && (projectileController.transform.position - vector).sqrMagnitude < num)
			{
				list.Add(projectileController);
			}
		}
		int j = 0;
		for (int count2 = list.Count; j < count2; j++)
		{
			ProjectileController projectileController2 = list[j];
			if (!projectileController2)
			{
				continue;
			}
			Vector3 position = projectileController2.transform.position;
			Vector3 start = vector;
			if ((bool)tracerEffectPrefab)
			{
				EffectData effectData = new EffectData
				{
					origin = position,
					start = start
				};
				EffectManager.SpawnEffect(tracerEffectPrefab, effectData, transmit: true);
			}
			_ = projectileController2.owner;
			CharacterBody component = projectileController2.owner.GetComponent<CharacterBody>();
			projectileController2.IgnoreCollisionsWithOwner(shouldIgnore: false);
			projectileController2.Networkowner = base.gameObject;
			projectileController2.teamFilter.teamIndex = base.characterBody.teamComponent.teamIndex;
			ProjectileDamage component2 = projectileController2.GetComponent<ProjectileDamage>();
			if ((bool)component2)
			{
				component2.damage *= damageMultiplier;
			}
			Rigidbody component3 = projectileController2.GetComponent<Rigidbody>();
			if ((bool)component3)
			{
				Vector3 vector2 = component3.velocity * -1f;
				if ((bool)component)
				{
					vector2 = component.corePosition - component3.transform.position;
				}
				component3.transform.forward = vector2;
				component3.velocity = Vector3.RotateTowards(component3.velocity, vector2, float.PositiveInfinity, 0f);
			}
		}
	}

	private void PushEnemies()
	{
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.teamMaskFilter = TeamMask.all;
		bullseyeSearch.maxAngleFilter = fieldOfView * 0.5f;
		bullseyeSearch.maxDistanceFilter = maxKnockbackDistance;
		bullseyeSearch.searchOrigin = aimRay.origin;
		bullseyeSearch.searchDirection = aimRay.direction;
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
		bullseyeSearch.filterByLoS = false;
		bullseyeSearch.RefreshCandidates();
		bullseyeSearch.FilterOutGameObject(base.gameObject);
		IEnumerable<HurtBox> enumerable = bullseyeSearch.GetResults().Where(Util.IsValid).Distinct(default(HurtBox.EntityEqualityComparer));
		TeamIndex team = GetTeam();
		foreach (HurtBox item in enumerable)
		{
			if (FriendlyFireManager.ShouldSplashHitProceed(item.healthComponent, team))
			{
				CharacterBody body = item.healthComponent.body;
				AddDebuff(body);
				body.RecalculateStats();
				_ = body.acceleration;
			}
		}
	}

	protected virtual void AddDebuff(CharacterBody body)
	{
		body.AddTimedBuff(buffDef, buffDuration);
		body.healthComponent.GetComponent<SetStateOnHurt>()?.SetStun(-1f);
	}

	protected virtual float CalculateDamage()
	{
		return 0f;
	}

	protected virtual float CalculateProcCoefficient()
	{
		return 0f;
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
		return InterruptPriority.PrioritySkill;
	}
}

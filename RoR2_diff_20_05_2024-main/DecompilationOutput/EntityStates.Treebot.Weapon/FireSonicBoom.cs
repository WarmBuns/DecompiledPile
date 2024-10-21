using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Treebot.Weapon;

public class FireSonicBoom : BaseState
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
	public float maxDistance;

	[SerializeField]
	public float idealDistanceToPlaceTargets;

	[SerializeField]
	public float liftVelocity;

	[SerializeField]
	public float slowDuration;

	[SerializeField]
	public float groundKnockbackDistance;

	[SerializeField]
	public float airKnockbackDistance;

	public static AnimationCurve shoveSuitabilityCurve;

	private float duration;

	private static int FireSonicBoomStateHash = Animator.StringToHash("FireSonicBoom");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("Gesture, Additive", FireSonicBoomStateHash);
		Util.PlaySound(sound, base.gameObject);
		Ray aimRay = GetAimRay();
		if (!string.IsNullOrEmpty(muzzle))
		{
			EffectManager.SimpleMuzzleFlash(fireEffectPrefab, base.gameObject, muzzle, transmit: false);
		}
		else
		{
			EffectManager.SpawnEffect(fireEffectPrefab, new EffectData
			{
				origin = aimRay.origin,
				rotation = Quaternion.LookRotation(aimRay.direction)
			}, transmit: false);
		}
		aimRay.origin -= aimRay.direction * backupDistance;
		if (NetworkServer.active)
		{
			BullseyeSearch bullseyeSearch = new BullseyeSearch();
			bullseyeSearch.teamMaskFilter = TeamMask.all;
			bullseyeSearch.maxAngleFilter = fieldOfView * 0.5f;
			bullseyeSearch.maxDistanceFilter = maxDistance;
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
					Vector3 vector = item.transform.position - aimRay.origin;
					float magnitude = vector.magnitude;
					_ = new Vector2(vector.x, vector.z).magnitude;
					Vector3 vector2 = vector / magnitude;
					float num = 1f;
					CharacterBody body = item.healthComponent.body;
					if ((bool)body.characterMotor)
					{
						num = body.characterMotor.mass;
					}
					else if ((bool)item.healthComponent.GetComponent<Rigidbody>())
					{
						num = base.rigidbody.mass;
					}
					float num2 = shoveSuitabilityCurve.Evaluate(num);
					AddDebuff(body);
					body.RecalculateStats();
					float acceleration = body.acceleration;
					Vector3 vector3 = vector2;
					float num3 = Trajectory.CalculateInitialYSpeedForHeight(Mathf.Abs(idealDistanceToPlaceTargets - magnitude), 0f - acceleration) * Mathf.Sign(idealDistanceToPlaceTargets - magnitude);
					vector3 *= num3;
					vector3.y = liftVelocity;
					DamageInfo damageInfo = new DamageInfo
					{
						attacker = base.gameObject,
						damage = CalculateDamage(),
						position = item.transform.position,
						procCoefficient = CalculateProcCoefficient()
					};
					item.healthComponent.TakeDamageForce(vector3 * (num * num2), alwaysApply: true, disableAirControlUntilCollision: true);
					item.healthComponent.TakeDamage(new DamageInfo
					{
						attacker = base.gameObject,
						damage = CalculateDamage(),
						position = item.transform.position,
						procCoefficient = CalculateProcCoefficient()
					});
					GlobalEventManager.instance.OnHitEnemy(damageInfo, item.healthComponent.gameObject);
				}
			}
		}
		if (base.isAuthority && (bool)base.characterBody && (bool)base.characterBody.characterMotor)
		{
			float height = (base.characterBody.characterMotor.isGrounded ? groundKnockbackDistance : airKnockbackDistance);
			float num4 = (base.characterBody.characterMotor ? base.characterBody.characterMotor.mass : 1f);
			float acceleration2 = base.characterBody.acceleration;
			float num5 = Trajectory.CalculateInitialYSpeedForHeight(height, 0f - acceleration2);
			base.characterBody.characterMotor.ApplyForce((0f - num5) * num4 * aimRay.direction);
		}
	}

	protected virtual void AddDebuff(CharacterBody body)
	{
		body.AddTimedBuff(RoR2Content.Buffs.Weak, slowDuration);
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

using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidJailer.Weapon;

public class Capture2 : BaseState
{
	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateName;

	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public float pullFieldOfView;

	[SerializeField]
	public float pullMinDistance;

	[SerializeField]
	public float pullMaxDistance;

	[SerializeField]
	public AnimationCurve pullSuitabilityCurve;

	[SerializeField]
	public GameObject pullTracerPrefab;

	[SerializeField]
	public float pullLiftVelocity;

	[SerializeField]
	public BuffDef debuffDef;

	[SerializeField]
	public float debuffDuration;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public float procCoefficient;

	[SerializeField]
	public GameObject muzzleflashEffectPrefab;

	[SerializeField]
	public string muzzleString;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateName, duration);
		Util.PlaySound(enterSoundString, base.gameObject);
		if ((bool)muzzleflashEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: false);
		}
		Ray aimRay = GetAimRay();
		if (!NetworkServer.active)
		{
			return;
		}
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.teamMaskFilter = TeamMask.all;
		bullseyeSearch.maxAngleFilter = pullFieldOfView * 0.5f;
		bullseyeSearch.maxDistanceFilter = pullMaxDistance;
		bullseyeSearch.searchOrigin = aimRay.origin;
		bullseyeSearch.searchDirection = aimRay.direction;
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.Angle;
		bullseyeSearch.filterByLoS = true;
		bullseyeSearch.RefreshCandidates();
		bullseyeSearch.FilterOutGameObject(base.gameObject);
		HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault();
		GetTeam();
		if (!hurtBox)
		{
			return;
		}
		Vector3 vector = hurtBox.transform.position - aimRay.origin;
		float magnitude = vector.magnitude;
		Vector3 vector2 = vector / magnitude;
		float num = 1f;
		CharacterBody body = hurtBox.healthComponent.body;
		if ((bool)body.characterMotor)
		{
			num = body.characterMotor.mass;
		}
		else if ((bool)hurtBox.healthComponent.GetComponent<Rigidbody>())
		{
			num = base.rigidbody.mass;
		}
		if ((bool)debuffDef)
		{
			body.AddTimedBuff(debuffDef, debuffDuration);
		}
		float num2 = pullSuitabilityCurve.Evaluate(num);
		Vector3 vector3 = vector2;
		float num3 = Trajectory.CalculateInitialYSpeedForHeight(Mathf.Abs(pullMinDistance - magnitude)) * Mathf.Sign(pullMinDistance - magnitude);
		vector3 *= num3;
		vector3.y = pullLiftVelocity;
		DamageInfo damageInfo = new DamageInfo
		{
			attacker = base.gameObject,
			damage = damageStat * damageCoefficient,
			position = hurtBox.transform.position,
			procCoefficient = procCoefficient
		};
		hurtBox.healthComponent.TakeDamageForce(vector3 * (num * num2), alwaysApply: true, disableAirControlUntilCollision: true);
		hurtBox.healthComponent.TakeDamage(damageInfo);
		GlobalEventManager.instance.OnHitEnemy(damageInfo, hurtBox.healthComponent.gameObject);
		if ((bool)pullTracerPrefab)
		{
			Vector3 position = hurtBox.transform.position;
			Vector3 start = base.characterBody.corePosition;
			Transform transform = FindModelChild(muzzleString);
			if ((bool)transform)
			{
				start = transform.position;
			}
			EffectData effectData = new EffectData
			{
				origin = position,
				start = start
			};
			EffectManager.SpawnEffect(pullTracerPrefab, effectData, transmit: true);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextState(new ExitCapture());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

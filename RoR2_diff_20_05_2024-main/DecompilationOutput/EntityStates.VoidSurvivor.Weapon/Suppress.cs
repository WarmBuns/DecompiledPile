using System.Linq;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidSurvivor.Weapon;

public class Suppress : BaseSkillState
{
	[SerializeField]
	public float minimumDuration;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public float maxSearchAngleFilter;

	[SerializeField]
	public float maxSearchDistance;

	[SerializeField]
	public float idealDistance;

	[SerializeField]
	public float springConstant;

	[SerializeField]
	public float springMaxLength;

	[SerializeField]
	public float damperConstant;

	[SerializeField]
	public float suppressedTargetForceRadius;

	[SerializeField]
	public GameObject suppressEffectPrefab;

	[SerializeField]
	public string muzzle;

	[SerializeField]
	public AnimationCurve forceSuitabilityCurve;

	[SerializeField]
	public float damageCoefficientPerSecond;

	[SerializeField]
	public float procCoefficientPerSecond;

	[SerializeField]
	public float corruptionPerSecond;

	[SerializeField]
	public float maxBreakDistance;

	[SerializeField]
	public float tickRate;

	[SerializeField]
	public bool applyForces;

	private GameObject suppressEffectInstance;

	private Transform idealFXTransform;

	private Transform targetFXTransform;

	private Transform muzzleTransform;

	private VoidSurvivorController voidSurvivorController;

	private CharacterBody targetBody;

	private float damageTickCountdown;

	public override void OnEnter()
	{
		base.OnEnter();
		targetBody = null;
		voidSurvivorController = GetComponent<VoidSurvivorController>();
		PlayAnimation(animationLayerName, animationStateName);
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				muzzleTransform = component.FindChild(muzzle);
				if ((bool)muzzleTransform && (bool)suppressEffectPrefab)
				{
					suppressEffectInstance = Object.Instantiate(suppressEffectPrefab, muzzleTransform.position, muzzleTransform.rotation);
					suppressEffectInstance.transform.parent = base.characterBody.transform;
					ChildLocator component2 = suppressEffectInstance.GetComponent<ChildLocator>();
					if ((bool)component2)
					{
						idealFXTransform = component2.FindChild("IdealFX");
						targetFXTransform = component2.FindChild("TargetFX");
					}
				}
			}
		}
		Ray aimRay = GetAimRay();
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(GetTeam());
		bullseyeSearch.maxAngleFilter = maxSearchAngleFilter;
		bullseyeSearch.maxDistanceFilter = maxSearchDistance;
		bullseyeSearch.searchOrigin = aimRay.origin;
		bullseyeSearch.searchDirection = aimRay.direction;
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.Angle;
		bullseyeSearch.filterByLoS = true;
		bullseyeSearch.RefreshCandidates();
		bullseyeSearch.FilterOutGameObject(base.gameObject);
		HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault();
		if ((bool)hurtBox)
		{
			Debug.LogFormat("Found target {0}", targetBody);
			targetBody = hurtBox.healthComponent.body;
		}
	}

	public override void OnExit()
	{
		if ((bool)suppressEffectInstance)
		{
			EntityState.Destroy(suppressEffectInstance);
		}
		base.OnExit();
	}

	public override void Update()
	{
		base.Update();
		if ((bool)targetBody)
		{
			if ((bool)muzzleTransform)
			{
				suppressEffectInstance.transform.position = muzzleTransform.position;
			}
			if ((bool)idealFXTransform)
			{
				Ray aimRay = GetAimRay();
				_ = targetBody.corePosition;
				Vector3 position = aimRay.origin + aimRay.direction * idealDistance;
				idealFXTransform.position = position;
			}
			if ((bool)targetFXTransform)
			{
				targetFXTransform.position = targetBody.corePosition;
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		base.characterBody.SetAimTimer(3f);
		float deltaTime = GetDeltaTime();
		if (NetworkServer.active && (bool)targetBody)
		{
			Ray aimRay = GetAimRay();
			Vector3 corePosition = targetBody.corePosition;
			Vector3 vector = aimRay.origin + aimRay.direction * idealDistance;
			if (applyForces)
			{
				Vector3 vector2 = vector - corePosition;
				float magnitude = vector2.magnitude;
				Mathf.Clamp01(magnitude / suppressedTargetForceRadius);
				Vector3 velocity;
				float mass;
				bool flag;
				if ((bool)targetBody.characterMotor)
				{
					velocity = targetBody.characterMotor.velocity;
					mass = targetBody.characterMotor.mass;
					flag = !targetBody.characterMotor.isFlying;
				}
				else
				{
					Rigidbody obj = targetBody.rigidbody;
					velocity = obj.velocity;
					mass = obj.mass;
					flag = obj.useGravity;
				}
				Vector3 vector3 = vector2.normalized * Mathf.Min(springMaxLength, magnitude) * springConstant * deltaTime;
				Vector3 vector4 = -velocity * damperConstant * deltaTime;
				Vector3 vector5 = (flag ? (Physics.gravity * deltaTime * mass) : Vector3.zero);
				float num = forceSuitabilityCurve.Evaluate(mass);
				targetBody.healthComponent.TakeDamageForce((vector3 + vector4) * num - vector5, alwaysApply: true, disableAirControlUntilCollision: true);
			}
			damageTickCountdown -= deltaTime;
			if (damageTickCountdown <= 0f)
			{
				damageTickCountdown = 1f / tickRate;
				DamageInfo damageInfo = new DamageInfo();
				damageInfo.attacker = base.gameObject;
				damageInfo.procCoefficient = procCoefficientPerSecond / tickRate;
				damageInfo.damage = damageCoefficientPerSecond * damageStat / tickRate;
				damageInfo.crit = RollCrit();
				targetBody.healthComponent.TakeDamage(damageInfo);
				voidSurvivorController.AddCorruption(corruptionPerSecond / tickRate);
			}
		}
		if (base.isAuthority && (!targetBody || !targetBody.healthComponent.alive))
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Paladin;

public class DashSlam : BaseState
{
	private float stopwatch;

	public static float damageCoefficient = 4f;

	public static float baseForceMagnitude = 16f;

	public static float bonusImpactForce;

	public static string initialAttackSoundString;

	public static GameObject chargeEffectPrefab;

	public static GameObject slamEffectPrefab;

	public static GameObject hitEffectPrefab;

	public static float initialSpeedCoefficient;

	public static float finalSpeedCoefficient;

	public static float duration;

	public static float overlapSphereRadius;

	public static float blastAttackRadius;

	private BlastAttack attack;

	private Transform modelTransform;

	private GameObject leftHandChargeEffect;

	private GameObject rightHandChargeEffect;

	private ChildLocator modelChildLocator;

	private Vector3 initialAimVector;

	private void EnableIndicator(string childLocatorName, ChildLocator childLocator = null)
	{
		if (!childLocator)
		{
			childLocator = GetModelTransform().GetComponent<ChildLocator>();
		}
		Transform transform = childLocator.FindChild(childLocatorName);
		if ((bool)transform)
		{
			transform.gameObject.SetActive(value: true);
			ObjectScaleCurve component = transform.gameObject.GetComponent<ObjectScaleCurve>();
			if ((bool)component)
			{
				component.time = 0f;
			}
		}
	}

	private void DisableIndicator(string childLocatorName, ChildLocator childLocator = null)
	{
		if (!childLocator)
		{
			childLocator = GetModelTransform().GetComponent<ChildLocator>();
		}
		Transform transform = childLocator.FindChild(childLocatorName);
		if ((bool)transform)
		{
			transform.gameObject.SetActive(value: false);
		}
	}

	public override void OnEnter()
	{
		base.OnEnter();
		modelTransform = GetModelTransform();
		Util.PlaySound(initialAttackSoundString, base.gameObject);
		initialAimVector = Vector3.ProjectOnPlane(GetAimRay().direction, Vector3.up);
		base.characterMotor.velocity.y = 0f;
		base.characterDirection.forward = initialAimVector;
		attack = new BlastAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = TeamComponent.GetObjectTeam(attack.attacker);
		attack.baseDamage = damageCoefficient * damageStat;
		attack.damageType = DamageType.Stun1s;
		attack.baseForce = baseForceMagnitude;
		attack.radius = blastAttackRadius + base.characterBody.radius;
		attack.falloffModel = BlastAttack.FalloffModel.None;
		attack.attackerFiltering = AttackerFiltering.NeverHitSelf;
		if (!modelTransform)
		{
			return;
		}
		modelChildLocator = modelTransform.GetComponent<ChildLocator>();
		if ((bool)modelChildLocator)
		{
			GameObject original = chargeEffectPrefab;
			Transform transform = modelChildLocator.FindChild("HandL");
			Transform transform2 = modelChildLocator.FindChild("HandR");
			if ((bool)transform)
			{
				leftHandChargeEffect = Object.Instantiate(original, transform);
			}
			if ((bool)transform2)
			{
				rightHandChargeEffect = Object.Instantiate(original, transform2);
			}
			EnableIndicator("GroundSlamIndicator", modelChildLocator);
		}
	}

	public override void OnExit()
	{
		if (NetworkServer.active)
		{
			attack.position = base.transform.position;
			attack.bonusForce = (initialAimVector + Vector3.up * 0.3f) * bonusImpactForce;
			attack.Fire();
		}
		if (base.isAuthority && (bool)modelTransform)
		{
			EffectManager.SimpleMuzzleFlash(slamEffectPrefab, base.gameObject, "SlamZone", transmit: true);
		}
		EntityState.Destroy(leftHandChargeEffect);
		EntityState.Destroy(rightHandChargeEffect);
		DisableIndicator("GroundSlamIndicator", modelChildLocator);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (base.isAuthority)
		{
			Collider[] colliders;
			int num = HGPhysics.OverlapSphere(out colliders, base.transform.position, base.characterBody.radius + overlapSphereRadius, LayerIndex.entityPrecise.mask);
			for (int i = 0; i < num; i++)
			{
				HurtBox component = colliders[i].GetComponent<HurtBox>();
				if ((bool)component && component.healthComponent != base.healthComponent)
				{
					HGPhysics.ReturnResults(colliders);
					outer.SetNextStateToMain();
					return;
				}
			}
		}
		if ((bool)base.characterMotor)
		{
			float num2 = Mathf.Lerp(initialSpeedCoefficient, finalSpeedCoefficient, stopwatch / duration) * base.characterBody.moveSpeed;
			Vector3 velocity = new Vector3(initialAimVector.x * num2, 0f, initialAimVector.z * num2);
			base.characterMotor.velocity = velocity;
			base.characterMotor.moveDirection = initialAimVector;
		}
		if (stopwatch > duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

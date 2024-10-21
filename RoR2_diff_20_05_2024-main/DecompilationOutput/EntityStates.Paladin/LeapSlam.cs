using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Paladin;

public class LeapSlam : BaseState
{
	private float stopwatch;

	public static float damageCoefficient = 4f;

	public static float forceMagnitude = 16f;

	public static float yBias;

	public static string initialAttackSoundString;

	public static GameObject chargeEffectPrefab;

	public static GameObject slamEffectPrefab;

	public static GameObject hitEffectPrefab;

	public static float leapVelocityCoefficient;

	public static float verticalLeapBonusCoefficient;

	public static float minimumDuration;

	private float leapVelocity;

	private OverlapAttack attack;

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
		leapVelocity = base.characterBody.moveSpeed * leapVelocityCoefficient;
		modelTransform = GetModelTransform();
		Util.PlaySound(initialAttackSoundString, base.gameObject);
		initialAimVector = GetAimRay().direction;
		initialAimVector.y = Mathf.Max(initialAimVector.y, 0f);
		initialAimVector.y += yBias;
		initialAimVector = initialAimVector.normalized;
		base.characterMotor.velocity.y = leapVelocity * initialAimVector.y * verticalLeapBonusCoefficient;
		attack = new OverlapAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = TeamComponent.GetObjectTeam(attack.attacker);
		attack.damage = damageCoefficient * damageStat;
		attack.hitEffectPrefab = hitEffectPrefab;
		attack.damageType = DamageType.Stun1s;
		attack.forceVector = Vector3.up * forceMagnitude;
		if ((bool)modelTransform)
		{
			attack.hitBoxGroup = Array.Find(modelTransform.GetComponents<HitBoxGroup>(), (HitBoxGroup element) => element.groupName == "GroundSlam");
		}
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
				leftHandChargeEffect = UnityEngine.Object.Instantiate(original, transform);
			}
			if ((bool)transform2)
			{
				rightHandChargeEffect = UnityEngine.Object.Instantiate(original, transform2);
			}
			EnableIndicator("GroundSlamIndicator", modelChildLocator);
		}
	}

	public override void OnExit()
	{
		if (NetworkServer.active)
		{
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
		if ((bool)base.characterMotor)
		{
			Vector3 velocity = base.characterMotor.velocity;
			Vector3 velocity2 = new Vector3(initialAimVector.x * leapVelocity, velocity.y, initialAimVector.z * leapVelocity);
			base.characterMotor.velocity = velocity2;
			base.characterMotor.moveDirection = initialAimVector;
		}
		if (base.characterMotor.isGrounded && stopwatch > minimumDuration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

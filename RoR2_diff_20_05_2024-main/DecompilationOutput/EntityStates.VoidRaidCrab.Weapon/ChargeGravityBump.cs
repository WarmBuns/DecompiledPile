using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace EntityStates.VoidRaidCrab.Weapon;

public class ChargeGravityBump : BaseGravityBumpState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public GameObject chargeEffectPrefab;

	[SerializeField]
	public string muzzleName;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public bool isSoundScaledByAttackSpeed;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public float horizontalAirborneForce;

	[SerializeField]
	public float verticalAirborneForce;

	[SerializeField]
	public float horizontalGroundedForce;

	[SerializeField]
	public float verticalGroundedForce;

	[SerializeField]
	public GameObject forceIndicatorPrefab;

	private float duration;

	private GameObject chargeEffectInstance;

	private Dictionary<CharacterMotor, Transform> characterMotorsToIndicatorTransforms;

	private Quaternion airborneForceOrientation;

	private Quaternion groundedForceOrientation;

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			isLeft = Random.value > 0.5f;
			CharacterDirection component = GetComponent<CharacterDirection>();
			if ((bool)component)
			{
				Vector3 vector = Vector3.Cross(component.forward, Vector3.up);
				if (!isLeft)
				{
					vector *= -1f;
				}
				airborneForce = Vector3.up * -1f * verticalAirborneForce + vector * horizontalAirborneForce;
				groundedForce = Vector3.up * verticalGroundedForce + vector * horizontalGroundedForce;
			}
		}
		airborneForceOrientation = Util.QuaternionSafeLookRotation(airborneForce);
		groundedForceOrientation = Util.QuaternionSafeLookRotation(groundedForce);
		duration = baseDuration / attackSpeedStat;
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		ChildLocator modelChildLocator = GetModelChildLocator();
		if ((bool)modelChildLocator && (bool)chargeEffectPrefab)
		{
			Transform transform = modelChildLocator.FindChild(muzzleName) ?? base.characterBody.coreTransform;
			if ((bool)transform)
			{
				chargeEffectInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
				chargeEffectInstance.transform.parent = transform;
				ScaleParticleSystemDuration component2 = chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
				if ((bool)component2)
				{
					component2.newDuration = duration;
				}
			}
		}
		if (!string.IsNullOrEmpty(enterSoundString))
		{
			if (isSoundScaledByAttackSpeed)
			{
				Util.PlayAttackSpeedSound(enterSoundString, base.gameObject, attackSpeedStat);
			}
			else
			{
				Util.PlaySound(enterSoundString, base.gameObject);
			}
		}
		characterMotorsToIndicatorTransforms = new Dictionary<CharacterMotor, Transform>();
		AssignIndicators();
	}

	public override void OnExit()
	{
		CleanUpIndicators();
		EntityState.Destroy(chargeEffectInstance);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		foreach (KeyValuePair<CharacterMotor, Transform> characterMotorsToIndicatorTransform in characterMotorsToIndicatorTransforms)
		{
			if (characterMotorsToIndicatorTransform.Key.isGrounded)
			{
				characterMotorsToIndicatorTransform.Value.rotation = groundedForceOrientation;
			}
			else
			{
				characterMotorsToIndicatorTransform.Value.rotation = airborneForceOrientation;
			}
		}
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextState(new FireGravityBump());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	private void AssignIndicators()
	{
		foreach (HurtBox target in GetTargets())
		{
			GameObject gameObject = target.healthComponent.gameObject;
			if ((bool)gameObject)
			{
				CharacterMotor component = gameObject.GetComponent<CharacterMotor>();
				if ((bool)component && !characterMotorsToIndicatorTransforms.ContainsKey(component))
				{
					GameObject gameObject2 = Object.Instantiate(forceIndicatorPrefab, gameObject.transform);
					characterMotorsToIndicatorTransforms.Add(component, gameObject2.transform);
				}
			}
		}
	}

	private void CleanUpIndicators()
	{
		foreach (KeyValuePair<CharacterMotor, Transform> characterMotorsToIndicatorTransform in characterMotorsToIndicatorTransforms)
		{
			Transform value = characterMotorsToIndicatorTransform.Value;
			if ((bool)value)
			{
				EntityState.Destroy(value.gameObject);
			}
		}
		characterMotorsToIndicatorTransforms.Clear();
	}
}

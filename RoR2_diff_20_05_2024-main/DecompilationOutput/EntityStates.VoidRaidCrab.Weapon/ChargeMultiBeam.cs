using RoR2;
using UnityEngine;

namespace EntityStates.VoidRaidCrab.Weapon;

public class ChargeMultiBeam : BaseMultiBeamState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public GameObject chargeEffectPrefab;

	[SerializeField]
	public GameObject warningLaserVfxPrefab;

	[SerializeField]
	public new string muzzleName;

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

	private float duration;

	private GameObject chargeEffectInstance;

	private GameObject warningLaserVfxInstance;

	private RayAttackIndicator warningLaserVfxInstanceRayAttackIndicator;

	private bool warningLaserEnabled
	{
		get
		{
			return warningLaserVfxInstance;
		}
		set
		{
			if (value == warningLaserEnabled)
			{
				return;
			}
			if (value)
			{
				if ((bool)warningLaserVfxPrefab)
				{
					warningLaserVfxInstance = Object.Instantiate(warningLaserVfxPrefab);
					warningLaserVfxInstanceRayAttackIndicator = warningLaserVfxInstance.GetComponent<RayAttackIndicator>();
					UpdateWarningLaser();
				}
			}
			else
			{
				EntityState.Destroy(warningLaserVfxInstance);
				warningLaserVfxInstance = null;
				warningLaserVfxInstanceRayAttackIndicator = null;
			}
		}
	}

	public override void OnEnter()
	{
		base.OnEnter();
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
				ScaleParticleSystemDuration component = chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
				if ((bool)component)
				{
					component.newDuration = duration;
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
		warningLaserEnabled = true;
	}

	public override void OnExit()
	{
		warningLaserEnabled = false;
		EntityState.Destroy(chargeEffectInstance);
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextState(new FireMultiBeamSmall());
		}
	}

	public override void Update()
	{
		base.Update();
		UpdateWarningLaser();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	private void UpdateWarningLaser()
	{
		if ((bool)warningLaserVfxInstanceRayAttackIndicator)
		{
			warningLaserVfxInstanceRayAttackIndicator.attackRange = BaseMultiBeamState.beamMaxDistance;
			CalcBeamPath(out var beamRay, out var _);
			warningLaserVfxInstanceRayAttackIndicator.attackRay = beamRay;
		}
	}
}

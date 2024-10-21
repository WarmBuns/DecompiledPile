using RoR2;
using RoR2.UI;
using UnityEngine;

namespace EntityStates.Commando.CommandoWeapon;

public class PrepLightsOut : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject chargePrefab;

	public static GameObject specialCrosshairPrefab;

	public static string prepSoundString;

	private GameObject chargeEffect;

	private float duration;

	private ChildLocator childLocator;

	private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

	private EffectManagerHelper _emh_chargeEffect;

	private static int PrepRevolverStateHash = Animator.StringToHash("PrepRevolver");

	private static int PrepRevolverParamHash = Animator.StringToHash("PrepRevolver.playbackRate");

	public override void Reset()
	{
		base.Reset();
		chargeEffect = null;
		duration = 0f;
		childLocator = null;
		_emh_chargeEffect = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("Gesture, Additive", PrepRevolverStateHash, PrepRevolverParamHash, duration);
		PlayAnimation("Gesture, Override", PrepRevolverStateHash, PrepRevolverParamHash, duration);
		Util.PlaySound(prepSoundString, base.gameObject);
		if ((bool)specialCrosshairPrefab)
		{
			crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, specialCrosshairPrefab, CrosshairUtils.OverridePriority.Skill);
		}
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			childLocator = modelTransform.GetComponent<ChildLocator>();
			if ((bool)childLocator)
			{
				Transform transform = childLocator.FindChild("MuzzlePistol");
				if ((bool)transform && (bool)chargePrefab)
				{
					if (!EffectManager.ShouldUsePooledEffect(chargePrefab))
					{
						chargeEffect = Object.Instantiate(chargePrefab, transform.position, transform.rotation);
					}
					else
					{
						_emh_chargeEffect = EffectManager.GetAndActivatePooledEffect(chargePrefab, transform.position, transform.rotation);
						chargeEffect = _emh_chargeEffect.gameObject;
					}
					chargeEffect.transform.parent = transform;
					ScaleParticleSystemDuration component = chargeEffect.GetComponent<ScaleParticleSystemDuration>();
					if ((bool)component)
					{
						component.newDuration = duration;
					}
				}
			}
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextState(new FireLightsOut());
		}
	}

	public override void OnExit()
	{
		if (_emh_chargeEffect != null && _emh_chargeEffect.OwningPool != null)
		{
			_emh_chargeEffect.OwningPool.ReturnObject(_emh_chargeEffect);
		}
		else
		{
			EntityState.Destroy(chargeEffect);
		}
		chargeEffect = null;
		_emh_chargeEffect = null;
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

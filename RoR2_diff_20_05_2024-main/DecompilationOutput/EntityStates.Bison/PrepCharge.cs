using RoR2;
using UnityEngine;

namespace EntityStates.Bison;

public class PrepCharge : BaseState
{
	public static float basePrepDuration;

	public static string enterSoundString;

	public static GameObject chargeEffectPrefab;

	private float stopwatch;

	private float prepDuration;

	private GameObject chargeEffectInstance;

	private EffectManagerHelper _emh_chargeEffectInstance;

	public override void Reset()
	{
		base.Reset();
		prepDuration = 0f;
		chargeEffectInstance = null;
		_emh_chargeEffectInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		prepDuration = basePrepDuration / attackSpeedStat;
		base.characterBody.SetAimTimer(prepDuration);
		PlayCrossfade("Body", "PrepCharge", "PrepCharge.playbackRate", prepDuration, 0.2f);
		Util.PlaySound(enterSoundString, base.gameObject);
		Transform modelTransform = GetModelTransform();
		AimAnimator component = modelTransform.GetComponent<AimAnimator>();
		if ((bool)component)
		{
			component.enabled = true;
		}
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = GetAimRay().direction;
		}
		if (!modelTransform)
		{
			return;
		}
		ChildLocator component2 = modelTransform.GetComponent<ChildLocator>();
		if (!component2)
		{
			return;
		}
		Transform transform = component2.FindChild("ChargeIndicator");
		if ((bool)transform && (bool)chargeEffectPrefab)
		{
			if (!EffectManager.ShouldUsePooledEffect(chargeEffectPrefab))
			{
				chargeEffectInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
			}
			else
			{
				_emh_chargeEffectInstance = EffectManager.GetAndActivatePooledEffect(chargeEffectPrefab, transform.position, transform.rotation);
				chargeEffectInstance = _emh_chargeEffectInstance.gameObject;
			}
			chargeEffectInstance.transform.parent = transform;
			ScaleParticleSystemDuration component3 = chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component3)
			{
				component3.newDuration = prepDuration;
			}
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		if ((bool)chargeEffectInstance)
		{
			if (_emh_chargeEffectInstance != null && _emh_chargeEffectInstance.OwningPool != null)
			{
				_emh_chargeEffectInstance.OwningPool.ReturnObject(_emh_chargeEffectInstance);
			}
			else
			{
				EntityState.Destroy(chargeEffectInstance);
			}
			chargeEffectInstance = null;
			_emh_chargeEffectInstance = null;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch > prepDuration && base.isAuthority)
		{
			outer.SetNextState(new Charge());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

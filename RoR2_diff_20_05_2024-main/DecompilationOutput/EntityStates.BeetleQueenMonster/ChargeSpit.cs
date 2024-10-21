using RoR2;
using UnityEngine;

namespace EntityStates.BeetleQueenMonster;

public class ChargeSpit : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject effectPrefab;

	public static string attackSoundString;

	private float duration;

	private GameObject chargeEffect;

	private EffectManagerHelper _emh_chargeEffect;

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		chargeEffect = null;
		_emh_chargeEffect = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		PlayCrossfade("Gesture", "ChargeSpit", "ChargeSpit.playbackRate", duration, 0.2f);
		Util.PlaySound(attackSoundString, base.gameObject);
		if (!modelTransform)
		{
			return;
		}
		ChildLocator component = modelTransform.GetComponent<ChildLocator>();
		if (!component || !effectPrefab)
		{
			return;
		}
		Transform transform = component.FindChild("Mouth");
		if ((bool)transform)
		{
			if (!EffectManager.ShouldUsePooledEffect(effectPrefab))
			{
				chargeEffect = Object.Instantiate(effectPrefab, transform.position, transform.rotation);
			}
			else
			{
				_emh_chargeEffect = EffectManager.GetAndActivatePooledEffect(effectPrefab, transform.position, transform.rotation);
				chargeEffect = _emh_chargeEffect.gameObject;
			}
			chargeEffect.transform.parent = transform;
			ScaleParticleSystemDuration component2 = chargeEffect.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component2)
			{
				component2.newDuration = duration;
			}
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		if (chargeEffect != null)
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
		}
	}

	public override void Update()
	{
		base.Update();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = GetAimRay().direction;
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			FireSpit nextState = new FireSpit();
			outer.SetNextState(nextState);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}

using RoR2;
using UnityEngine;

namespace EntityStates.LemurianBruiserMonster;

public class ChargeMegaFireball : BaseState
{
	public static float baseDuration = 1f;

	public static GameObject chargeEffectPrefab;

	public static string attackString;

	private float duration;

	private GameObject chargeInstance;

	private EffectManagerHelper _emh_chargeInstance;

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		chargeInstance = null;
		_emh_chargeInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Animator modelAnimator = GetModelAnimator();
		Transform modelTransform = GetModelTransform();
		Util.PlayAttackSpeedSound(attackString, base.gameObject, attackSpeedStat);
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild("MuzzleMouth");
				if ((bool)transform && (bool)chargeEffectPrefab)
				{
					if (!EffectManager.ShouldUsePooledEffect(chargeEffectPrefab))
					{
						chargeInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
					}
					else
					{
						_emh_chargeInstance = EffectManager.GetAndActivatePooledEffect(chargeEffectPrefab, transform.position, transform.rotation);
						chargeInstance = _emh_chargeInstance.gameObject;
					}
					chargeInstance.transform.parent = transform;
					ScaleParticleSystemDuration component2 = chargeInstance.GetComponent<ScaleParticleSystemDuration>();
					if ((bool)component2)
					{
						component2.newDuration = duration;
					}
				}
			}
		}
		if ((bool)modelAnimator)
		{
			PlayCrossfade("Gesture, Additive", "ChargeMegaFireball", "ChargeMegaFireball.playbackRate", duration, 0.1f);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		if ((bool)chargeInstance)
		{
			if (_emh_chargeInstance != null && _emh_chargeInstance.OwningPool != null)
			{
				_emh_chargeInstance.OwningPool.ReturnObject(_emh_chargeInstance);
			}
			else
			{
				EntityState.Destroy(chargeInstance);
			}
			chargeInstance = null;
			_emh_chargeInstance = null;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			FireMegaFireball nextState = new FireMegaFireball();
			outer.SetNextState(nextState);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}

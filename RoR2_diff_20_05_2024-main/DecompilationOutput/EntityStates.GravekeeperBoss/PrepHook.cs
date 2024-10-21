using RoR2;
using UnityEngine;

namespace EntityStates.GravekeeperBoss;

public class PrepHook : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject chargeEffectPrefab;

	public static string muzzleString;

	public static string attackString;

	private float duration;

	private GameObject chargeInstance;

	private Animator modelAnimator;

	private EffectManagerHelper _emh_chargeInstance;

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		chargeInstance = null;
		modelAnimator = null;
		_emh_chargeInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		base.fixedAge = 0f;
		duration = baseDuration / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			PlayCrossfade("Body", "PrepHook", "PrepHook.playbackRate", duration, 0.5f);
			modelAnimator.GetComponent<AimAnimator>().enabled = true;
		}
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = base.inputBank.aimDirection;
		}
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild(muzzleString);
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
		Util.PlayAttackSpeedSound(attackString, base.gameObject, attackSpeedStat);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextState(new FireHook());
		}
	}

	public override void OnExit()
	{
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
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}

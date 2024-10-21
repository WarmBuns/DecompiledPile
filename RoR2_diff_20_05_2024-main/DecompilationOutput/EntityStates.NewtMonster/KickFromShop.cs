using RoR2;
using UnityEngine;

namespace EntityStates.NewtMonster;

public class KickFromShop : BaseState
{
	public static float duration = 3.5f;

	public static string attackSoundString;

	public static string stompSoundString;

	public static GameObject chargeEffectPrefab;

	public static GameObject stompEffectPrefab;

	private Animator modelAnimator;

	private Transform modelTransform;

	private bool hasAttacked;

	private GameObject chargeEffectInstance;

	private EffectManagerHelper _emh_chargeEffectInstance;

	public override void Reset()
	{
		base.Reset();
		modelAnimator = null;
		modelTransform = null;
		hasAttacked = false;
		chargeEffectInstance = null;
		_emh_chargeEffectInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
		modelTransform = GetModelTransform();
		Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, attackSpeedStat);
		PlayCrossfade("Body", "Stomp", "Stomp.playbackRate", duration, 0.1f);
		if (!modelTransform)
		{
			return;
		}
		ChildLocator component = modelTransform.GetComponent<ChildLocator>();
		if (!component)
		{
			return;
		}
		Transform transform = component.FindChild("StompMuzzle");
		if ((bool)transform)
		{
			if (!EffectManager.ShouldUsePooledEffect(chargeEffectPrefab))
			{
				chargeEffectInstance = Object.Instantiate(chargeEffectPrefab, transform);
				return;
			}
			_emh_chargeEffectInstance = EffectManager.GetAndActivatePooledEffect(chargeEffectPrefab, transform, inResetLocal: true);
			chargeEffectInstance = _emh_chargeEffectInstance.gameObject;
		}
	}

	protected void DestroyChargeEffect()
	{
		if (chargeEffectInstance != null)
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

	public override void OnExit()
	{
		DestroyChargeEffect();
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)modelAnimator && modelAnimator.GetFloat("Stomp.hitBoxActive") > 0.5f && !hasAttacked)
		{
			Util.PlayAttackSpeedSound(stompSoundString, base.gameObject, attackSpeedStat);
			EffectManager.SimpleMuzzleFlash(stompEffectPrefab, base.gameObject, "HealthBarOrigin", transmit: false);
			if ((bool)SceneInfo.instance)
			{
				GameObject gameObject = SceneInfo.instance.transform.Find("KickOutOfShop").gameObject;
				if ((bool)gameObject)
				{
					gameObject.gameObject.SetActive(value: true);
				}
			}
			if (base.isAuthority)
			{
				HurtBoxGroup component = modelTransform.GetComponent<HurtBoxGroup>();
				if ((bool)component)
				{
					int hurtBoxesDeactivatorCounter = component.hurtBoxesDeactivatorCounter + 1;
					component.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
				}
			}
			hasAttacked = true;
			DestroyChargeEffect();
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

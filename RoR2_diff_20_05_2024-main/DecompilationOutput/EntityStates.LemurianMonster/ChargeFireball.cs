using RoR2;
using UnityEngine;

namespace EntityStates.LemurianMonster;

public class ChargeFireball : BaseState
{
	public static float baseDuration = 1f;

	public static GameObject chargeVfxPrefab;

	public static string attackString;

	private float duration;

	private GameObject chargeVfxInstance;

	protected EffectManagerHelper _efhChargeEffect;

	private static int ChargeFireballStateHash = Animator.StringToHash("ChargeFireball");

	private static int ChargeFireballParamHash = Animator.StringToHash("ChargeFireball.playbackRate");

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		chargeVfxInstance = null;
		_efhChargeEffect = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		GetModelAnimator();
		Transform modelTransform = GetModelTransform();
		Util.PlayAttackSpeedSound(attackString, base.gameObject, attackSpeedStat);
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild("MuzzleMouth");
				if ((bool)transform)
				{
					if (!EffectManager.ShouldUsePooledEffect(chargeVfxPrefab))
					{
						chargeVfxInstance = Object.Instantiate(chargeVfxPrefab, transform.position, transform.rotation);
					}
					else
					{
						_efhChargeEffect = EffectManager.GetAndActivatePooledEffect(chargeVfxPrefab, transform.position, transform.rotation);
						chargeVfxInstance = _efhChargeEffect.gameObject;
					}
					chargeVfxInstance.transform.parent = transform;
				}
			}
		}
		PlayAnimation("Gesture", ChargeFireballStateHash, ChargeFireballParamHash, duration);
	}

	public override void OnExit()
	{
		base.OnExit();
		if (!chargeVfxInstance)
		{
			return;
		}
		if (!EffectManager.UsePools)
		{
			EntityState.Destroy(chargeVfxInstance);
			return;
		}
		if (_efhChargeEffect != null && _efhChargeEffect.OwningPool != null)
		{
			_efhChargeEffect.OwningPool.ReturnObject(_efhChargeEffect);
			return;
		}
		if (_efhChargeEffect != null)
		{
			Debug.LogFormat("ChargeFireball has no owning pool {0} {1}", base.gameObject.name, base.gameObject.GetInstanceID());
		}
		EntityState.Destroy(chargeVfxInstance);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			FireFireball nextState = new FireFireball();
			outer.SetNextState(nextState);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}

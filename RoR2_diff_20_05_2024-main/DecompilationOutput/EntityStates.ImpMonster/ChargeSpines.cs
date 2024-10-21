using RoR2;
using UnityEngine;

namespace EntityStates.ImpMonster;

public class ChargeSpines : BaseState
{
	public static float baseDuration = 1f;

	public static GameObject effectPrefab;

	private float duration;

	private GameObject chargeEffect;

	private EffectManagerHelper _emh_chargeEffect;

	private static int ChargeSpinesStateHash = Animator.StringToHash("ChargeSpines");

	private static int ChargeSpinesParamHash = Animator.StringToHash("ChargeSpines.playbackRate");

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
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild("MuzzleMouth");
				if ((bool)transform && (bool)effectPrefab)
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
				}
			}
		}
		PlayAnimation("Gesture", ChargeSpinesStateHash, ChargeSpinesParamHash, duration);
	}

	public override void OnExit()
	{
		base.OnExit();
		if ((bool)chargeEffect)
		{
			if (_emh_chargeEffect != null && _emh_chargeEffect.OwningPool != null)
			{
				_emh_chargeEffect.OwningPool.ReturnObject(_emh_chargeEffect);
			}
			else
			{
				EntityState.Destroy(chargeEffect);
			}
			_emh_chargeEffect = null;
			chargeEffect = null;
		}
	}

	public override void Update()
	{
		base.Update();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			FireSpines nextState = new FireSpines();
			outer.SetNextState(nextState);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}

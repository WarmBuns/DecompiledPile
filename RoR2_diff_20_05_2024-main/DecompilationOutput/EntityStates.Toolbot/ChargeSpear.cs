using RoR2;
using UnityEngine;

namespace EntityStates.Toolbot;

public class ChargeSpear : BaseToolbotPrimarySkillState
{
	public static float baseMinChargeDuration;

	public static float baseChargeDuration;

	public static float perfectChargeWindow;

	public static GameObject chargeupVfxPrefab;

	public static GameObject holdChargeVfxPrefab;

	private float minChargeDuration;

	private float chargeDuration;

	private bool released;

	private GameObject chargeupVfxGameObject;

	private GameObject holdChargeVfxGameObject;

	private EffectManagerHelper _emh_chargeupVfx;

	private EffectManagerHelper _emh_holdChargeVfx;

	private static int ChargeSpearStateHash = Animator.StringToHash("ChargeSpear");

	private static int ChargeSpearParamHash = Animator.StringToHash("ChargeSpear.playbackRate");

	public override void Reset()
	{
		base.Reset();
		minChargeDuration = 0f;
		chargeDuration = 0f;
		released = false;
		chargeupVfxGameObject = null;
		holdChargeVfxGameObject = null;
		_emh_chargeupVfx = null;
		_emh_holdChargeVfx = null;
		released = false;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		minChargeDuration = baseMinChargeDuration / attackSpeedStat;
		chargeDuration = baseChargeDuration / attackSpeedStat;
		if (!base.isInDualWield)
		{
			PlayAnimation("Gesture, Additive", ChargeSpearStateHash, ChargeSpearParamHash, chargeDuration);
		}
		if ((bool)base.muzzleTransform)
		{
			if (!EffectManager.ShouldUsePooledEffect(chargeupVfxPrefab))
			{
				chargeupVfxGameObject = Object.Instantiate(chargeupVfxPrefab, base.muzzleTransform);
			}
			else
			{
				_emh_chargeupVfx = EffectManager.GetAndActivatePooledEffect(chargeupVfxPrefab, base.muzzleTransform, inResetLocal: true);
				chargeupVfxGameObject = _emh_chargeupVfx.gameObject;
			}
			chargeupVfxGameObject.GetComponent<ScaleParticleSystemDuration>().newDuration = chargeDuration;
		}
	}

	protected void KillChargeEffect()
	{
		if (!chargeupVfxGameObject)
		{
			return;
		}
		if (!EffectManager.UsePools)
		{
			EntityState.Destroy(chargeupVfxGameObject);
		}
		else
		{
			if (_emh_chargeupVfx != null && _emh_chargeupVfx.OwningPool != null)
			{
				_emh_chargeupVfx.OwningPool.ReturnObject(_emh_chargeupVfx);
			}
			else
			{
				EntityState.Destroy(chargeupVfxGameObject);
			}
			_emh_chargeupVfx = null;
		}
		chargeupVfxGameObject = null;
	}

	public override void OnExit()
	{
		KillChargeEffect();
		if ((bool)holdChargeVfxGameObject)
		{
			if (!EffectManager.UsePools)
			{
				EntityState.Destroy(holdChargeVfxGameObject);
			}
			else
			{
				if (_emh_holdChargeVfx != null && _emh_holdChargeVfx.OwningPool != null)
				{
					_emh_holdChargeVfx.OwningPool.ReturnObject(_emh_holdChargeVfx);
				}
				else
				{
					EntityState.Destroy(holdChargeVfxGameObject);
				}
				_emh_holdChargeVfx = null;
			}
			holdChargeVfxGameObject = null;
		}
		base.OnExit();
	}

	public override void Update()
	{
		base.Update();
		base.characterBody.SetSpreadBloom(base.age / chargeDuration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		float num = base.fixedAge - chargeDuration;
		if (num >= 0f)
		{
			_ = perfectChargeWindow;
		}
		float charge = Mathf.Clamp01(base.fixedAge / chargeDuration);
		if (base.fixedAge >= chargeDuration)
		{
			KillChargeEffect();
			if (!holdChargeVfxGameObject && (bool)base.muzzleTransform)
			{
				if (!EffectManager.ShouldUsePooledEffect(holdChargeVfxPrefab))
				{
					holdChargeVfxGameObject = Object.Instantiate(holdChargeVfxPrefab, base.muzzleTransform);
				}
				else
				{
					_emh_holdChargeVfx = EffectManager.GetAndActivatePooledEffect(holdChargeVfxPrefab, base.muzzleTransform, inResetLocal: true);
					holdChargeVfxGameObject = _emh_holdChargeVfx.gameObject;
				}
			}
		}
		if (base.isAuthority)
		{
			if (!released && !IsKeyDownAuthority())
			{
				released = true;
			}
			if (released && base.fixedAge >= minChargeDuration)
			{
				outer.SetNextState(new FireSpear
				{
					charge = charge,
					activatorSkillSlot = base.activatorSkillSlot
				});
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}

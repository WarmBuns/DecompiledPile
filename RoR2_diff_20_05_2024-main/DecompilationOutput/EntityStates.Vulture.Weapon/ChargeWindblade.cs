using RoR2;
using UnityEngine;

namespace EntityStates.Vulture.Weapon;

public class ChargeWindblade : BaseSkillState
{
	public static float baseDuration;

	public static string muzzleString;

	public static GameObject chargeEffectPrefab;

	public static string soundString;

	private float duration;

	private GameObject chargeEffectInstance;

	protected EffectManagerHelper _emh_chargeEffectInstance;

	private static int ChargeWindbladeStateHash = Animator.StringToHash("ChargeWindblade");

	private static int ChargeWindbladeParamHash = Animator.StringToHash("ChargeWindblade.playbackRate");

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		chargeEffectInstance = null;
		_emh_chargeEffectInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("Gesture, Additive", ChargeWindbladeStateHash, ChargeWindbladeParamHash, duration);
		Util.PlaySound(soundString, base.gameObject);
		base.characterBody.SetAimTimer(3f);
		Transform transform = FindModelChild(muzzleString);
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
			ScaleParticleSystemDuration component = chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component)
			{
				component.newDuration = duration;
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(new FireWindblade());
		}
	}

	public override void OnExit()
	{
		if (_emh_chargeEffectInstance != null && _emh_chargeEffectInstance.OwningPool != null)
		{
			_emh_chargeEffectInstance.OwningPool.ReturnObject(_emh_chargeEffectInstance);
		}
		else if (chargeEffectInstance != null)
		{
			EntityState.Destroy(chargeEffectInstance);
		}
		chargeEffectInstance = null;
		_emh_chargeEffectInstance = null;
		base.OnExit();
	}
}

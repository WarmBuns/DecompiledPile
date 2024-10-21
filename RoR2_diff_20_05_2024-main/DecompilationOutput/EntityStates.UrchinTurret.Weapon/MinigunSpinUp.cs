using RoR2;
using UnityEngine;

namespace EntityStates.UrchinTurret.Weapon;

public class MinigunSpinUp : MinigunState
{
	public static float baseDuration;

	public static string sound;

	public static GameObject chargeEffectPrefab;

	private GameObject chargeInstance;

	private float duration;

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
		Util.PlaySound(sound, base.gameObject);
		PlayCrossfade("Gesture, Additive", "EnterShootLoop", 0.2f);
		if ((bool)muzzleTransform && (bool)chargeEffectPrefab)
		{
			if (!EffectManager.ShouldUsePooledEffect(chargeEffectPrefab))
			{
				chargeInstance = Object.Instantiate(chargeEffectPrefab, muzzleTransform.position, muzzleTransform.rotation);
			}
			else
			{
				_emh_chargeInstance = EffectManager.GetAndActivatePooledEffect(chargeEffectPrefab, muzzleTransform.position, muzzleTransform.rotation);
				chargeInstance = _emh_chargeInstance.gameObject;
			}
			chargeInstance.transform.parent = muzzleTransform;
			ScaleParticleSystemDuration component = chargeInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component)
			{
				component.newDuration = duration;
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextState(new MinigunFire());
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
			_emh_chargeInstance = null;
			chargeInstance = null;
		}
	}
}

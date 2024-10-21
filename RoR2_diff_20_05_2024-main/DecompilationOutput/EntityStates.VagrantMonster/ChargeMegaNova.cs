using RoR2;
using UnityEngine;

namespace EntityStates.VagrantMonster;

public class ChargeMegaNova : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject chargingEffectPrefab;

	public static GameObject areaIndicatorPrefab;

	public static string chargingSoundString;

	public static float novaRadius;

	private float duration;

	private float stopwatch;

	private GameObject chargeEffectInstance;

	private GameObject areaIndicatorInstance;

	private uint soundID;

	private EffectManagerHelper _emh_chargeEffectInstance;

	private EffectManagerHelper _emh_areaIndicatorInstance;

	private Vector3 _cachedScaleVector = Vector3.one;

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		stopwatch = 0f;
		chargeEffectInstance = null;
		areaIndicatorInstance = null;
		soundID = 0u;
		_emh_chargeEffectInstance = null;
		_emh_areaIndicatorInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		duration = baseDuration / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		PlayCrossfade("Gesture, Override", "ChargeMegaNova", "ChargeMegaNova.playbackRate", duration, 0.3f);
		soundID = Util.PlayAttackSpeedSound(chargingSoundString, base.gameObject, attackSpeedStat);
		if (!modelTransform)
		{
			return;
		}
		ChildLocator component = modelTransform.GetComponent<ChildLocator>();
		if (!component)
		{
			return;
		}
		Transform transform = component.FindChild("HullCenter");
		Transform transform2 = component.FindChild("NovaCenter");
		if ((bool)transform && (bool)chargingEffectPrefab)
		{
			if (!EffectManager.ShouldUsePooledEffect(chargingEffectPrefab))
			{
				chargeEffectInstance = Object.Instantiate(chargingEffectPrefab, transform.position, transform.rotation);
			}
			else
			{
				_emh_chargeEffectInstance = EffectManager.GetAndActivatePooledEffect(chargingEffectPrefab, transform.position, transform.rotation);
				chargeEffectInstance = _emh_chargeEffectInstance.gameObject;
			}
			_cachedScaleVector.x = novaRadius;
			_cachedScaleVector.y = novaRadius;
			_cachedScaleVector.z = novaRadius;
			chargeEffectInstance.transform.localScale = _cachedScaleVector;
			chargeEffectInstance.transform.parent = transform;
			chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>().newDuration = duration;
		}
		if ((bool)transform2 && (bool)areaIndicatorPrefab)
		{
			if (!EffectManager.ShouldUsePooledEffect(areaIndicatorPrefab))
			{
				areaIndicatorInstance = Object.Instantiate(areaIndicatorPrefab, transform2.position, transform2.rotation);
			}
			else
			{
				_emh_areaIndicatorInstance = EffectManager.GetAndActivatePooledEffect(areaIndicatorPrefab, transform2.position, transform2.rotation);
				areaIndicatorInstance = _emh_areaIndicatorInstance.gameObject;
			}
			_cachedScaleVector.x = novaRadius * 2f;
			_cachedScaleVector.y = novaRadius * 2f;
			_cachedScaleVector.z = novaRadius * 2f;
			areaIndicatorInstance.transform.localScale = _cachedScaleVector;
			areaIndicatorInstance.transform.parent = transform2;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		AkSoundEngine.StopPlayingID(soundID);
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
			_emh_chargeEffectInstance = null;
			chargeEffectInstance = null;
		}
		if ((bool)areaIndicatorInstance)
		{
			if (_emh_areaIndicatorInstance != null && _emh_areaIndicatorInstance.OwningPool != null)
			{
				_emh_areaIndicatorInstance.OwningPool.ReturnObject(_emh_areaIndicatorInstance);
			}
			else
			{
				EntityState.Destroy(areaIndicatorInstance);
			}
			_emh_areaIndicatorInstance = null;
			areaIndicatorInstance = null;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration && base.isAuthority)
		{
			FireMegaNova fireMegaNova = new FireMegaNova();
			fireMegaNova.novaRadius = novaRadius;
			outer.SetNextState(fireMegaNova);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}
}

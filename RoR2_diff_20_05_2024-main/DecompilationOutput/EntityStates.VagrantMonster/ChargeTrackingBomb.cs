using RoR2;
using UnityEngine;

namespace EntityStates.VagrantMonster;

public class ChargeTrackingBomb : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject chargingEffectPrefab;

	public static string chargingSoundString;

	private float duration;

	private float stopwatch;

	private GameObject chargeEffectInstance;

	private uint soundID;

	private EffectManagerHelper _emh_chargeEffectInstance;

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		stopwatch = 0f;
		chargeEffectInstance = null;
		soundID = 0u;
		_emh_chargeEffectInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		duration = baseDuration / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		PlayCrossfade("Gesture, Override", "ChargeTrackingBomb", "ChargeTrackingBomb.playbackRate", duration, 0.3f);
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
		Transform transform = component.FindChild("TrackingBombMuzzle");
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
			chargeEffectInstance.transform.parent = transform;
			chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>().newDuration = duration;
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
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextState(new FireTrackingBomb());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}
}

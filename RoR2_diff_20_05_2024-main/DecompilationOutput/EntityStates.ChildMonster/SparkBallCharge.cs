using RoR2;
using UnityEngine;

namespace EntityStates.ChildMonster;

public class SparkBallCharge : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject chargingEffectPrefab;

	public static string chargingSoundString;

	private float duration;

	private float stopwatch;

	private GameObject chargeEffectInstance;

	private uint soundID;

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		duration = baseDuration / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		PlayCrossfade("Gesture, Override", "SparkEnter", "SparkEnter.playbackRate", duration, 0.3f);
		soundID = Util.PlayAttackSpeedSound(chargingSoundString, base.gameObject, attackSpeedStat);
		if (!modelTransform)
		{
			return;
		}
		ChildLocator component = modelTransform.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			Transform transform = component.FindChild("MuzzleFire");
			if ((bool)transform && (bool)chargingEffectPrefab)
			{
				chargeEffectInstance = Object.Instantiate(chargingEffectPrefab, transform.position, transform.rotation);
				chargeEffectInstance.transform.parent = transform;
				chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>().newDuration = duration;
			}
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		AkSoundEngine.StopPlayingID(soundID);
		if ((bool)chargeEffectInstance)
		{
			EntityState.Destroy(chargeEffectInstance);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextState(new SparkBallFire());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}
}

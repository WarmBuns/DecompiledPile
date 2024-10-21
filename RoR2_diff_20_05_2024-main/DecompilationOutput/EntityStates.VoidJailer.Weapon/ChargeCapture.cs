using RoR2;
using UnityEngine;

namespace EntityStates.VoidJailer.Weapon;

public class ChargeCapture : BaseState
{
	public static string animationLayerName;

	public static string animationStateName;

	public static string animationPlaybackRateName;

	public static float duration;

	public static string enterSoundString;

	public static GameObject chargeEffectPrefab;

	public static GameObject attackIndicatorPrefab;

	public static string muzzleString;

	private float _crossFadeDuration;

	private uint soundID;

	private GameObject attackIndicatorInstance;

	public override void OnEnter()
	{
		base.OnEnter();
		duration /= attackSpeedStat;
		_crossFadeDuration = duration * 0.25f;
		PlayCrossfade(animationLayerName, animationStateName, animationPlaybackRateName, duration, _crossFadeDuration);
		soundID = Util.PlayAttackSpeedSound(enterSoundString, base.gameObject, attackSpeedStat);
		if ((bool)chargeEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(chargeEffectPrefab, base.gameObject, muzzleString, transmit: false);
		}
		if ((bool)attackIndicatorPrefab)
		{
			Transform coreTransform = base.characterBody.coreTransform;
			if ((bool)coreTransform)
			{
				attackIndicatorInstance = Object.Instantiate(attackIndicatorPrefab, coreTransform);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextState(new Capture2());
		}
	}

	public override void Update()
	{
		if ((bool)attackIndicatorInstance)
		{
			attackIndicatorInstance.transform.forward = GetAimRay().direction;
		}
		base.Update();
	}

	public override void OnExit()
	{
		AkSoundEngine.StopPlayingID(soundID);
		if ((bool)attackIndicatorInstance)
		{
			EntityState.Destroy(attackIndicatorInstance);
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

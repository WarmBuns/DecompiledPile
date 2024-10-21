using RoR2;
using UnityEngine;

namespace EntityStates.VoidSurvivor.Weapon;

public class ChargeHandBeam : BaseState
{
	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public float baseDuration = 2f;

	[SerializeField]
	public string muzzle;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public GameObject muzzleflashEffectPrefab;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		GetAimRay();
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		base.characterBody.SetAimTimer(3f);
		Util.PlaySound(enterSoundString, base.gameObject);
		if ((bool)muzzleflashEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzle, transmit: false);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge > duration)
		{
			outer.SetNextState(new FireHandBeam());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}

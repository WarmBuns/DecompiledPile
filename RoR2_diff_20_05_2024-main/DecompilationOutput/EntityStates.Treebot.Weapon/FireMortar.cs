using RoR2;
using UnityEngine;

namespace EntityStates.Treebot.Weapon;

public class FireMortar : BaseState
{
	public static GameObject muzzleEffectPrefab;

	public static string fireSoundString;

	public static float baseDuration;

	private float duration;

	private static int FireBombStateHash = Animator.StringToHash("FireBomb");

	private static int FireBombParamHash = Animator.StringToHash("FireBomb.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("Gesture, Additive", FireBombStateHash, FireBombParamHash, duration);
		Util.PlaySound(fireSoundString, base.gameObject);
		if ((bool)muzzleEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, "MuzzleNailgun", transmit: false);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

using UnityEngine;

namespace EntityStates.AncientWispMonster;

public class FireBomb : BaseState
{
	public static float baseDuration = 4f;

	private float duration;

	private static int fireBombHash = Animator.StringToHash("FireBomb");

	private static int fireBombParamHash = Animator.StringToHash("FireBomb.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("Gesture", fireBombHash, fireBombParamHash, duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}

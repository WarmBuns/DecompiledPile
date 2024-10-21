using RoR2;
using UnityEngine;

namespace EntityStates;

public class PrepFlower2 : BaseState
{
	public static float baseDuration;

	public static string enterSoundString;

	private float duration;

	private static int PrepFlowerStateHash = Animator.StringToHash("PrepFlower");

	private static int PrepFlowerParamHash = Animator.StringToHash("PrepFlower.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Util.PlaySound(enterSoundString, base.gameObject);
		PlayAnimation("Gesture, Additive", PrepFlowerStateHash, PrepFlowerParamHash, duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(new FireFlower2());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

using RoR2;
using UnityEngine;

namespace EntityStates.Treebot;

public class TreebotPrepFruitSeed : BaseState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public string animationLayerName = "Gesture, Additive";

	[SerializeField]
	public string animationStateName = "PrepFlower";

	[SerializeField]
	public string playbackRateParam = "PrepFlower.playbackRate";

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Util.PlaySound(enterSoundString, base.gameObject);
		PlayAnimation(animationLayerName, animationStateName, playbackRateParam, duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(new TreebotFireFruitSeed());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

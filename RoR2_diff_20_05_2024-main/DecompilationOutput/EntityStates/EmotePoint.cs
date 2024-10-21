using UnityEngine;

namespace EntityStates;

public class EmotePoint : BaseState
{
	public static float duration = 0.5f;

	private static int EmotePointStateHash = Animator.StringToHash("EmotePoint");

	private static int EmotePointplaybackRateParamHash = Animator.StringToHash("EmotePoint.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			int layerIndex = modelAnimator.GetLayerIndex("Gesture");
			modelAnimator.SetFloat(EmotePointplaybackRateParamHash, 1f);
			modelAnimator.PlayInFixedTime(EmotePointStateHash, layerIndex, 0f);
			modelAnimator.Update(0f);
			modelAnimator.SetFloat(EmotePointplaybackRateParamHash, attackSpeedStat);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}

using UnityEngine;

namespace EntityStates.Merc;

public class WhirlwindGround : WhirlwindBase
{
	private static int WhirlwindGroundStateHash = Animator.StringToHash("WhirlwindGround");

	private static int LightImpactStateHash = Animator.StringToHash("LightImpact");

	private static int WhirlwindParamHash = Animator.StringToHash("Whirlwind.playbackRate");

	protected override void PlayAnim()
	{
		PlayCrossfade("FullBody, Override", WhirlwindGroundStateHash, WhirlwindParamHash, duration, 0.1f);
	}

	public override void OnExit()
	{
		base.OnExit();
		int layerIndex = animator.GetLayerIndex("Impact");
		if (layerIndex >= 0)
		{
			animator.SetLayerWeight(layerIndex, 3f);
			PlayAnimation("Impact", LightImpactStateHash);
		}
	}
}

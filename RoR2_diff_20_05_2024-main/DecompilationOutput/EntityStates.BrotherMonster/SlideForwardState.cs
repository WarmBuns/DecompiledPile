using UnityEngine;

namespace EntityStates.BrotherMonster;

public class SlideForwardState : BaseSlideState
{
	public override void OnEnter()
	{
		slideRotation = Quaternion.identity;
		base.OnEnter();
		PlayCrossfade("FullBody Override", "SlideForward", "Slide.playbackRate", BaseSlideState.duration, 0.05f);
		PlayCrossfade("Body", "Run", 0.05f);
	}

	public override void OnExit()
	{
		base.OnExit();
	}
}

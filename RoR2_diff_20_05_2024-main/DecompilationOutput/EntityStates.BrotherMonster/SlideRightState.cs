using UnityEngine;

namespace EntityStates.BrotherMonster;

public class SlideRightState : BaseSlideState
{
	public override void OnEnter()
	{
		slideRotation = Quaternion.AngleAxis(90f, Vector3.up);
		base.OnEnter();
		PlayCrossfade("FullBody Override", "SlideRight", "Slide.playbackRate", BaseSlideState.duration, 0.05f);
	}
}

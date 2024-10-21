using UnityEngine;

namespace EntityStates.BrotherMonster;

public class SlideBackwardState : BaseSlideState
{
	public override void OnEnter()
	{
		slideRotation = Quaternion.AngleAxis(-180f, Vector3.up);
		base.OnEnter();
		PlayCrossfade("FullBody Override", "SlideBackward", "Slide.playbackRate", BaseSlideState.duration, 0.05f);
	}
}

using UnityEngine;

namespace EntityStates.BrotherMonster;

public class SlideLeftState : BaseSlideState
{
	public override void OnEnter()
	{
		slideRotation = Quaternion.AngleAxis(-90f, Vector3.up);
		base.OnEnter();
		PlayCrossfade("FullBody Override", "SlideLeft", "Slide.playbackRate", BaseSlideState.duration, 0.05f);
	}
}

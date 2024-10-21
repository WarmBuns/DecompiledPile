using UnityEngine;

namespace EntityStates.BrotherMonster;

public class StaggerExit : StaggerBaseState
{
	private static int StaggerExitStateHash = Animator.StringToHash("StaggerExit");

	private static int StaggerParamHash = Animator.StringToHash("Stagger.playbackRate");

	public override EntityState nextState => new GenericCharacterMain();

	public override void OnEnter()
	{
		base.OnEnter();
		PlayCrossfade("Body", StaggerExitStateHash, StaggerParamHash, duration, 0.1f);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}
}

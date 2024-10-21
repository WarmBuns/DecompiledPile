using UnityEngine;

namespace EntityStates.BrotherMonster;

public class StaggerLoop : StaggerBaseState
{
	private static int StaggerLoopStateHash = Animator.StringToHash("StaggerLoop");

	public override EntityState nextState => new StaggerExit();

	public override void OnEnter()
	{
		base.OnEnter();
		PlayCrossfade("Body", "StaggerLoop", 0.2f);
		PlayCrossfade("Body", StaggerLoopStateHash, 0.2f);
	}
}

using UnityEngine;

namespace EntityStates.SurvivorPod;

public class ReleaseFinished : SurvivorPodBaseState
{
	private static int ReleaseStateHash = Animator.StringToHash("Release");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Base", ReleaseStateHash);
	}
}

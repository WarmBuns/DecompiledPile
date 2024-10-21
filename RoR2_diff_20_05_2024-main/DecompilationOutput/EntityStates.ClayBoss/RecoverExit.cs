using UnityEngine;

namespace EntityStates.ClayBoss;

public class RecoverExit : BaseState
{
	public static float exitDuration = 1f;

	private float stopwatch;

	private static int ExitSiphonStateHash = Animator.StringToHash("ExitSiphon");

	private static int ExitSiphonParamHash = Animator.StringToHash("ExitSiphon.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		PlayAnimation("Body", ExitSiphonStateHash, ExitSiphonParamHash, exitDuration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= exitDuration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}

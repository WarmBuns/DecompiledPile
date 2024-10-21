using UnityEngine;

namespace EntityStates.Merc;

public class WhirlwindAir : WhirlwindBase
{
	private static int WhirlwindAirExitStateHash = Animator.StringToHash("WhirlwindAirExit");

	private static int WhirlwindAirStateHash = Animator.StringToHash("WhirlwindAir");

	private static int WhirlwindParamHash = Animator.StringToHash("Whirlwind.playbackRate");

	protected override void PlayAnim()
	{
		PlayCrossfade("FullBody, Override", WhirlwindAirStateHash, WhirlwindParamHash, duration, 0.1f);
	}

	public override void OnExit()
	{
		base.OnExit();
		PlayAnimation("FullBody, Override", WhirlwindAirExitStateHash);
	}
}

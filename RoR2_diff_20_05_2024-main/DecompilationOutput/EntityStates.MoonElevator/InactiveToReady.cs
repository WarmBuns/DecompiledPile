using RoR2;
using UnityEngine;

namespace EntityStates.MoonElevator;

public class InactiveToReady : MoonElevatorBaseState
{
	private static int InactiveToActiveStateHash = Animator.StringToHash("InactiveToActive");

	private static int playbackRateParamHash = Animator.StringToHash("playbackRate");

	public override Interactability interactability => Interactability.Disabled;

	public override bool goToNextStateAutomatically => true;

	public override EntityState nextState => new Ready();

	public override bool showBaseEffects => true;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Base", InactiveToActiveStateHash, playbackRateParamHash, duration);
	}
}

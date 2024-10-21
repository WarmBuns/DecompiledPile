using RoR2;
using UnityEngine;

namespace EntityStates.MoonElevator;

public class Inactive : MoonElevatorBaseState
{
	private static int InactiveStateHash = Animator.StringToHash("Inactive");

	public override Interactability interactability => Interactability.ConditionsNotMet;

	public override bool goToNextStateAutomatically => false;

	public override bool showBaseEffects => false;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Base", InactiveStateHash);
	}
}

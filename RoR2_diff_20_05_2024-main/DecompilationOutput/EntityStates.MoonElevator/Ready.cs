using RoR2;
using UnityEngine;

namespace EntityStates.MoonElevator;

public class Ready : MoonElevatorBaseState
{
	private static int ReadyStateHash = Animator.StringToHash("Ready");

	public override Interactability interactability => Interactability.Available;

	public override bool goToNextStateAutomatically => false;

	public override bool showBaseEffects => true;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Base", ReadyStateHash);
	}

	protected override void OnInteractionBegin(Interactor activator)
	{
		base.OnInteractionBegin(activator);
		_ = base.isAuthority;
	}
}

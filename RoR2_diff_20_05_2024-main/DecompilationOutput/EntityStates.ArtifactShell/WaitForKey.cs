using RoR2;

namespace EntityStates.ArtifactShell;

public class WaitForKey : ArtifactShellBaseState
{
	protected override CostTypeIndex interactionCostType => CostTypeIndex.ArtifactShellKillerItem;

	protected override int interactionCost => 1;

	protected override bool interactionAvailable => true;

	public override void OnEnter()
	{
		base.OnEnter();
	}

	protected override void OnPurchase(Interactor activator)
	{
		base.OnPurchase(activator);
		outer.SetInterruptState(new StartHurt(), InterruptPriority.Pain);
	}
}

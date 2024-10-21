using RoR2;

namespace EntityStates.Missions.ArtifactWorld.TrialController;

public class ArtifactTrialControllerBaseState : EntityState
{
	protected PurchaseInteraction purchaseInteraction;

	protected ChildLocator childLocator;

	public override void OnEnter()
	{
		base.OnEnter();
		purchaseInteraction = GetComponent<PurchaseInteraction>();
		childLocator = GetComponent<ChildLocator>();
	}
}

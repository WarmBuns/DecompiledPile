namespace EntityStates.Missions.ArtifactWorld.TrialController;

public class BeforeTrial : ArtifactTrialControllerBaseState
{
	public override void OnEnter()
	{
		base.OnEnter();
		purchaseInteraction.enabled = true;
		childLocator.FindChild("BeforeTrial").gameObject.SetActive(value: true);
	}

	public override void OnExit()
	{
		childLocator.FindChild("BeforeTrial").gameObject.SetActive(value: false);
		base.OnExit();
	}
}

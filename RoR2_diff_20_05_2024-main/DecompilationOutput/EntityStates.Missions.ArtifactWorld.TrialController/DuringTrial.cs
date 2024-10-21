namespace EntityStates.Missions.ArtifactWorld.TrialController;

public class DuringTrial : ArtifactTrialControllerBaseState
{
	public virtual EntityState GetNextState()
	{
		return new AfterTrial();
	}

	public override void OnEnter()
	{
		base.OnEnter();
		purchaseInteraction.enabled = false;
		childLocator.FindChild("DuringTrial").gameObject.SetActive(value: true);
	}

	public override void OnExit()
	{
		childLocator.FindChild("DuringTrial").gameObject.SetActive(value: false);
		base.OnExit();
	}
}

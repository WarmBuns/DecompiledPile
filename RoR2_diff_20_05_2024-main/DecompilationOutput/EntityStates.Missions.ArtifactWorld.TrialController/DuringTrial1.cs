namespace EntityStates.Missions.ArtifactWorld.TrialController;

public class DuringTrial1 : DuringTrial
{
	public static float trialDuration;

	public override EntityState GetNextState()
	{
		return new AfterTrial1();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= trialDuration)
		{
			outer.SetNextState(GetNextState());
		}
	}
}

using System;

namespace EntityStates.Missions.ArtifactWorld.TrialController;

public class AfterTrial1 : AfterTrial
{
	public override Type GetNextStateType()
	{
		return typeof(FinishTrial1);
	}
}

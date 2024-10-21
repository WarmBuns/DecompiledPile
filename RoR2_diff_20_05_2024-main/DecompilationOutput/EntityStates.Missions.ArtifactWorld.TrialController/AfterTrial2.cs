using System;

namespace EntityStates.Missions.ArtifactWorld.TrialController;

public class AfterTrial2 : AfterTrial
{
	public override Type GetNextStateType()
	{
		return typeof(FinishTrial2);
	}
}

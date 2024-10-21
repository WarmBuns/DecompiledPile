namespace EntityStates.Missions.BrotherEncounter;

public class EncounterFinished : BrotherEncounterBaseState
{
	protected override bool shouldEnableArenaNodes => false;

	public override void OnEnter()
	{
		base.OnEnter();
	}
}

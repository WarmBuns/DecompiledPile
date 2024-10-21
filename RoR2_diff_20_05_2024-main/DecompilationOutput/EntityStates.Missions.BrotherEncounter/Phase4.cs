namespace EntityStates.Missions.BrotherEncounter;

public class Phase4 : BrotherEncounterPhaseBaseState
{
	protected override string phaseControllerChildString => "Phase4";

	protected override EntityState nextState => new BossDeath();

	protected override float healthBarShowDelay => 6f;

	public override void OnEnter()
	{
		base.OnEnter();
		BeginEncounter();
	}
}

namespace EntityStates.Heretic;

public class SpawnOnNewStageState : SpawnState
{
	public override void OnEnter()
	{
		base.OnEnter();
		outer.SetNextStateToMain();
	}
}

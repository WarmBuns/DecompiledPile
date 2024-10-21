namespace EntityStates.VoidRaidCrab;

public class VacuumEnter : BaseVacuumAttackState
{
	protected override void OnLifetimeExpiredAuthority()
	{
		outer.SetNextState(new VacuumWindUp());
	}
}

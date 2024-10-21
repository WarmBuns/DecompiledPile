namespace EntityStates.VoidRaidCrab;

public class VacuumWindDown : BaseVacuumAttackState
{
	protected override void OnLifetimeExpiredAuthority()
	{
		outer.SetNextState(new VacuumExit());
	}
}

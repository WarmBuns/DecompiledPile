namespace EntityStates.VoidRaidCrab;

public class VacuumWindUp : BaseVacuumAttackState
{
	protected override void OnLifetimeExpiredAuthority()
	{
		outer.SetNextState(new VacuumAttack());
	}
}

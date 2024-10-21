namespace EntityStates.Bandit2.Weapon;

public class PrepSidearmResetRevolver : BasePrepSidearmRevolverState
{
	protected override EntityState GetNextState()
	{
		return new FireSidearmResetRevolver();
	}
}

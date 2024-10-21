namespace EntityStates.Bandit2.Weapon;

public class PrepSidearmSkullRevolver : BasePrepSidearmRevolverState
{
	protected override EntityState GetNextState()
	{
		return new FireSidearmSkullRevolver();
	}
}

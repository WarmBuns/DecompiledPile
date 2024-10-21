namespace EntityStates.RoboBallBoss.Weapon;

public class ChargeSuperEyeblast : ChargeEyeblast
{
	public override EntityState GetNextState()
	{
		return new FireSuperEyeblast();
	}
}

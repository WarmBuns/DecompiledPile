namespace EntityStates.VoidMegaCrab.Weapon;

public class ChargeCrabBlackCannon : ChargeCrabCannonBase
{
	protected override FireCrabCannonBase GetNextState()
	{
		return new FireCrabBlackCannon();
	}
}

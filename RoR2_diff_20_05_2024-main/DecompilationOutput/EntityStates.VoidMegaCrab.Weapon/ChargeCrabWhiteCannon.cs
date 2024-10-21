namespace EntityStates.VoidMegaCrab.Weapon;

public class ChargeCrabWhiteCannon : ChargeCrabCannonBase
{
	protected override FireCrabCannonBase GetNextState()
	{
		return new FireCrabWhiteCannon();
	}
}

namespace EntityStates.Mage.Weapon;

public class ChargeNovabomb : BaseChargeBombState
{
	protected override BaseThrowBombState GetNextState()
	{
		return new ThrowNovabomb();
	}
}

namespace EntityStates.Mage.Weapon;

public class ChargeIcebomb : BaseChargeBombState
{
	protected override BaseThrowBombState GetNextState()
	{
		return new ThrowIcebomb();
	}
}

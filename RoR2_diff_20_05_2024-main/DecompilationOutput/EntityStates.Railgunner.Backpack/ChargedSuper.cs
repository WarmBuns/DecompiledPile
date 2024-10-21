namespace EntityStates.Railgunner.Backpack;

public class ChargedSuper : BaseCharged
{
	protected override EntityState InstantiateExpiredState()
	{
		return new ExpiredSuper();
	}
}

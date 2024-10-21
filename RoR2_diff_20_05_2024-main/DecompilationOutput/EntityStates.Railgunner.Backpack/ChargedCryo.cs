namespace EntityStates.Railgunner.Backpack;

public class ChargedCryo : BaseCharged
{
	protected override EntityState InstantiateExpiredState()
	{
		return new ExpiredCryo();
	}
}

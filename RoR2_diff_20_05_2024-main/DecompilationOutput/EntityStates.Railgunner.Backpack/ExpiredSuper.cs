namespace EntityStates.Railgunner.Backpack;

public class ExpiredSuper : BaseExpired
{
	protected override EntityState InstantiateNextState()
	{
		return new OnlineSuper();
	}
}

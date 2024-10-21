namespace EntityStates.Railgunner.Backpack;

public class ExpiredCryo : BaseExpired
{
	protected override EntityState InstantiateNextState()
	{
		return new OnlineCryo();
	}
}

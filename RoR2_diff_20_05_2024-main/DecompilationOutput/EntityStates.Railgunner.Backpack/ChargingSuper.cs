namespace EntityStates.Railgunner.Backpack;

public class ChargingSuper : BaseCharging
{
	protected override EntityState InstantiateChargedState()
	{
		return new ChargedSuper();
	}
}

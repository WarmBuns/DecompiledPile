namespace EntityStates.Railgunner.Backpack;

public class ChargingCryo : BaseCharging
{
	protected override EntityState InstantiateChargedState()
	{
		return new ChargedCryo();
	}
}

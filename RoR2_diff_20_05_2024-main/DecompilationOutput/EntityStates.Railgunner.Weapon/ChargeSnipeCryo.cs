using EntityStates.Railgunner.Backpack;

namespace EntityStates.Railgunner.Weapon;

public class ChargeSnipeCryo : BaseChargeSnipe
{
	protected override EntityState InstantiateBackpackState()
	{
		return new ChargingCryo();
	}
}

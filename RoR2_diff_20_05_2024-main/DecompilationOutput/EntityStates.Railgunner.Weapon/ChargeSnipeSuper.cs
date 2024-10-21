using EntityStates.Railgunner.Backpack;

namespace EntityStates.Railgunner.Weapon;

public class ChargeSnipeSuper : BaseChargeSnipe
{
	protected override EntityState InstantiateBackpackState()
	{
		return new ChargingSuper();
	}
}

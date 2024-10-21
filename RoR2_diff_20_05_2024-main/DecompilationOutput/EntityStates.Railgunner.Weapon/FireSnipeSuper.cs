using EntityStates.Railgunner.Backpack;

namespace EntityStates.Railgunner.Weapon;

public class FireSnipeSuper : BaseFireSnipe
{
	protected override EntityState InstantiateBackpackState()
	{
		return new Offline();
	}
}

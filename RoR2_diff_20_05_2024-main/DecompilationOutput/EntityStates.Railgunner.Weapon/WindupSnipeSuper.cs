namespace EntityStates.Railgunner.Weapon;

public class WindupSnipeSuper : BaseWindupSnipe
{
	protected override EntityState InstantiateNextState()
	{
		return new FireSnipeSuper();
	}
}

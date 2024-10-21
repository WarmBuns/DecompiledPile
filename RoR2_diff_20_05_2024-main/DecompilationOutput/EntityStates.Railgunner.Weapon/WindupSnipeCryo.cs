namespace EntityStates.Railgunner.Weapon;

public class WindupSnipeCryo : BaseWindupSnipe
{
	protected override EntityState InstantiateNextState()
	{
		return new FireSnipeCryo();
	}
}

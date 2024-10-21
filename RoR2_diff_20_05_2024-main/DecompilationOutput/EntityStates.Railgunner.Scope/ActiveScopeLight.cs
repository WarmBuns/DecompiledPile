namespace EntityStates.Railgunner.Scope;

public class ActiveScopeLight : BaseActive
{
	protected override BaseWindDown GetNextState()
	{
		return new WindDownScopeLight();
	}
}

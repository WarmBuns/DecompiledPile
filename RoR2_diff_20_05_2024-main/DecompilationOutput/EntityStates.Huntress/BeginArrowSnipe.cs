namespace EntityStates.Huntress;

public class BeginArrowSnipe : BaseBeginArrowBarrage
{
	protected override EntityState InstantiateNextState()
	{
		return new AimArrowSnipe();
	}
}

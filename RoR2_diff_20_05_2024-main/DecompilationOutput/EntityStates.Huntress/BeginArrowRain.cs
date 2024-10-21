namespace EntityStates.Huntress;

public class BeginArrowRain : BaseBeginArrowBarrage
{
	protected override EntityState InstantiateNextState()
	{
		return new ArrowRain();
	}
}

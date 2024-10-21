namespace EntityStates.VoidRaidCrab.Leg;

public class PreStompLegRaise : BaseStompState
{
	protected override bool shouldUseWarningIndicator => true;

	protected override bool shouldUpdateLegStompTargetPosition => true;

	protected override void OnLifetimeExpiredAuthority()
	{
		outer.SetNextState(new PreStompFollowTarget
		{
			target = target
		});
	}
}

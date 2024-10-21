namespace EntityStates.VoidRaidCrab.Leg;

public class PreStompFollowTarget : BaseStompState
{
	protected override bool shouldUseWarningIndicator => true;

	protected override bool shouldUpdateLegStompTargetPosition => true;

	protected override void OnLifetimeExpiredAuthority()
	{
		outer.SetNextState(new PreStompPauseBeforeStomp
		{
			target = target
		});
	}
}

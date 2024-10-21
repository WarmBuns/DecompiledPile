namespace EntityStates.VoidRaidCrab.Leg;

public class PreStompPauseBeforeStomp : BaseStompState
{
	protected override void OnLifetimeExpiredAuthority()
	{
		outer.SetNextState(new Stomp
		{
			target = target
		});
	}
}

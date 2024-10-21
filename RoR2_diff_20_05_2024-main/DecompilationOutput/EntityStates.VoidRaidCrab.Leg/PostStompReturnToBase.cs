namespace EntityStates.VoidRaidCrab.Leg;

public class PostStompReturnToBase : BaseStompState
{
	protected override void OnLifetimeExpiredAuthority()
	{
		outer.SetNextStateToMain();
	}
}

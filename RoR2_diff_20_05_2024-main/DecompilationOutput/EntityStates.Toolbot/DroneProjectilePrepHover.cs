namespace EntityStates.Toolbot;

public class DroneProjectilePrepHover : BaseState
{
	public static float duration;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.age >= duration)
		{
			outer.SetNextStateToMain();
		}
	}
}

namespace EntityStates.LaserTurbine;

public class ReadyState : LaserTurbineBaseState
{
	public static float baseDuration;

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= baseDuration)
		{
			outer.SetNextState(new AimState());
		}
	}
}

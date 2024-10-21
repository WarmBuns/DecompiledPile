namespace EntityStates.Merc;

public class WhirlwindEntry : BaseState
{
	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			EntityState nextState = (((bool)base.characterMotor && base.characterMotor.isGrounded) ? ((WhirlwindBase)new WhirlwindGround()) : ((WhirlwindBase)new WhirlwindAir()));
			outer.SetNextState(nextState);
		}
	}
}

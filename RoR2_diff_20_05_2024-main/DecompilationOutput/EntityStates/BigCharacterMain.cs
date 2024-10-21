namespace EntityStates;

public class BigCharacterMain : GenericCharacterMain
{
	public override void ProcessJump()
	{
		if (base.characterMotor.jumpCount > base.characterBody.baseJumpCount)
		{
			base.ProcessJump();
		}
		else if (jumpInputReceived && base.characterMotor.isGrounded)
		{
			outer.SetNextState(new AnimatedJump());
		}
	}
}

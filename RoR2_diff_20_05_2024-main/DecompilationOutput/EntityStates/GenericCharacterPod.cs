namespace EntityStates;

public class GenericCharacterPod : BaseState
{
	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.characterMotor)
		{
			base.characterMotor.enabled = false;
		}
		if ((bool)base.rigidbodyMotor)
		{
			base.rigidbodyMotor.enabled = false;
		}
	}

	public override void OnExit()
	{
		if ((bool)base.characterMotor)
		{
			base.characterMotor.enabled = true;
		}
		if ((bool)base.rigidbodyMotor)
		{
			base.rigidbodyMotor.enabled = true;
		}
		base.OnExit();
	}
}

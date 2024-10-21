namespace EntityStates.Toolbot;

public abstract class AimGrenade : AimThrowableBase
{
	public override void OnEnter()
	{
		base.OnEnter();
		detonationRadius = 7f;
	}
}

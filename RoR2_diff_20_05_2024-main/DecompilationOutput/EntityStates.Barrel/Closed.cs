namespace EntityStates.Barrel;

public class Closed : EntityState
{
	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Body", "Closed");
	}
}

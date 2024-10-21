namespace EntityStates.AI.Walker;

public class Guard : LookBusy
{
	public override void FixedUpdate()
	{
		base.FixedUpdate();
		base.fixedAge = 0f;
	}

	protected override void PickNewTargetLookDirection()
	{
	}
}

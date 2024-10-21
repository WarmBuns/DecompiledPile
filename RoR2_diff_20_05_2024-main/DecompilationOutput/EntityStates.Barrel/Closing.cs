namespace EntityStates.Barrel;

public class Closing : EntityState
{
	public static float duration = 1f;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Body", "Closing", "Closing.playbackRate", duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(new Closed());
		}
	}
}

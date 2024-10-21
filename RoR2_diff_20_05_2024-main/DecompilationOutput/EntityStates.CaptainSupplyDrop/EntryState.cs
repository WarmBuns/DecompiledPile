namespace EntityStates.CaptainSupplyDrop;

public class EntryState : BaseCaptainSupplyDropState
{
	public static float baseDuration;

	private float duration;

	protected override bool shouldShowModel => false;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextState(new HitGroundState());
		}
	}
}

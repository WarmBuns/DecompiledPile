namespace EntityStates.Engi.SpiderMine;

public class PreDetonate : BaseSpiderMineState
{
	public static float baseDuration;

	private float duration;

	protected override bool shouldStick => false;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
		base.rigidbody.isKinematic = true;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && duration <= base.fixedAge)
		{
			outer.SetNextState(new Detonate());
		}
	}
}

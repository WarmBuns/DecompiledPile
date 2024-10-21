namespace EntityStates.Engi.SpiderMine;

public class Burrow : BaseSpiderMineState
{
	public static float baseDuration;

	private float duration;

	protected override bool shouldStick => true;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
		PlayAnimation("Base", BaseSpiderMineState.IdleToArmedStateHash, BaseSpiderMineState.IdleToArmedParamHash, duration);
		EmitDustEffect();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			EntityState entityState = null;
			if (!base.projectileStickOnImpact.stuck)
			{
				entityState = new WaitForStick();
			}
			else if (duration <= base.fixedAge)
			{
				entityState = new WaitForTarget();
			}
			if (entityState != null)
			{
				outer.SetNextState(entityState);
			}
		}
	}
}

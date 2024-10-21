namespace EntityStates.Engi.EngiMissilePainter;

public class Startup : BaseEngiMissilePainterState
{
	public static float baseDuration;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && duration <= base.fixedAge)
		{
			outer.SetNextState(new Paint());
		}
	}
}

namespace EntityStates.Engi.EngiMissilePainter;

public class Finish : BaseEngiMissilePainterState
{
	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			outer.SetNextState(new Idle());
		}
	}
}

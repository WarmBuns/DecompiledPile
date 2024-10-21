namespace EntityStates.Engi.EngiMissilePainter;

public class BaseEngiMissilePainterState : BaseSkillState
{
	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}
}

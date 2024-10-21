namespace EntityStates.FalseSon;

public class OverheadClubSwing : BasicMeleeAttack
{
	protected override void PlayAnimation()
	{
		PlayAnimation("FullBody, Override", "OverheadSwing", "ChargeSwing.playbackRate", duration);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}

using RoR2;

namespace EntityStates.Vermin.Weapon;

public class TongueLash : BasicMeleeAttack
{
	protected override void PlayAnimation()
	{
		PlayCrossfade("Gesture, Additive", "TongueLash", "TongueLash.playbackRate", duration, 0.1f);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}

	protected override void AuthorityModifyOverlapAttack(OverlapAttack overlapAttack)
	{
		base.AuthorityModifyOverlapAttack(overlapAttack);
	}
}

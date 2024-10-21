using UnityEngine;

namespace EntityStates.FalseSonBoss;

public class HeroRelicSwing : BasicMeleeAttack
{
	private float swingTimer;

	protected override void PlayAnimation()
	{
		string animationStateName = "SwingClubRight";
		float num = Mathf.Max(duration, 0.2f);
		PlayCrossfade("Gesture, Additive", animationStateName, "SwingClub.playbackRate", num, 0.1f);
		PlayCrossfade("Gesture, Override", animationStateName, "SwingClub.playbackRate", num, 0.1f);
	}

	protected override void BeginMeleeAttackEffect()
	{
		swingEffectMuzzleString = "SwingRight";
		base.BeginMeleeAttackEffect();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		swingTimer += Time.deltaTime;
		if (swingTimer > baseDuration)
		{
			outer.SetNextState(GetNextStateAuthority());
		}
	}

	protected virtual EntityState GetNextStateAuthority()
	{
		return new HeroRelicSwingLeft();
	}
}

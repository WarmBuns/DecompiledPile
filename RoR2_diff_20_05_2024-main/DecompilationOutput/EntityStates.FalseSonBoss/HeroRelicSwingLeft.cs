using UnityEngine;

namespace EntityStates.FalseSonBoss;

public class HeroRelicSwingLeft : BasicMeleeAttack
{
	private float swingTimer;

	protected override void PlayAnimation()
	{
		string animationStateName = "SwingClubLeft";
		float num = Mathf.Max(duration, 0.2f);
		PlayCrossfade("Gesture, Additive", animationStateName, "SwingClub.playbackRate", num, 0.1f);
		PlayCrossfade("Gesture, Override", animationStateName, "SwingClub.playbackRate", num, 0.1f);
	}

	protected override void BeginMeleeAttackEffect()
	{
		swingEffectMuzzleString = "SwingLeft";
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
			outer.SetNextStateToMain();
		}
	}
}

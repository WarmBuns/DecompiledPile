using RoR2;
using UnityEngine;

namespace EntityStates.ClayBoss;

public class PrepTarBall : BaseState
{
	public static float baseDuration = 3f;

	public static string prepTarBallSoundString;

	private float duration;

	private float stopwatch;

	private Animator modelAnimator;

	public override void OnEnter()
	{
		base.OnEnter();
		stopwatch = 0f;
		duration = baseDuration / attackSpeedStat;
		modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			PlayCrossfade("Body", "PrepTarBall", "PrepTarBall.playbackRate", duration, 0.5f);
		}
		if (!string.IsNullOrEmpty(prepTarBallSoundString))
		{
			Util.PlayAttackSpeedSound(prepTarBallSoundString, base.gameObject, attackSpeedStat);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextState(new FireTarball());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}

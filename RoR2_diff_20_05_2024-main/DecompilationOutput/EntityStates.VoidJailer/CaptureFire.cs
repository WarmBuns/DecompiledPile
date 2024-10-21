using RoR2;

namespace EntityStates.VoidJailer;

public class CaptureFire : BaseState
{
	public static string animationLayerName;

	public static string animationStateName;

	public static string animationPlaybackRateName;

	public static float duration;

	public static string enterSoundString;

	public static float exitCrossfadeDuration;

	public static string crossfadeStateName;

	public override void OnEnter()
	{
		base.OnEnter();
		duration /= attackSpeedStat;
		Util.PlaySound(enterSoundString, base.gameObject);
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateName, duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			Capture nextState = new Capture();
			outer.SetNextState(nextState);
		}
	}

	public override void OnExit()
	{
		PlayCrossfade(animationLayerName, crossfadeStateName, animationPlaybackRateName, exitCrossfadeDuration, exitCrossfadeDuration);
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

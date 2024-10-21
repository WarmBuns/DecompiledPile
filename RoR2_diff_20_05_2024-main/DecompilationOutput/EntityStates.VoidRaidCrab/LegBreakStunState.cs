namespace EntityStates.VoidRaidCrab;

public class LegBreakStunState : BaseState
{
	public static string animLayerName;

	public static string animStateName;

	public static string animPlaybackRateParamName;

	public static float baseDuration;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
		PlayAnimation(animLayerName, animStateName, animPlaybackRateParamName, duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}

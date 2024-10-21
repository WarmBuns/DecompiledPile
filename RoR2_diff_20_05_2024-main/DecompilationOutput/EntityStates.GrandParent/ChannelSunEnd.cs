namespace EntityStates.GrandParent;

public class ChannelSunEnd : ChannelSunBase
{
	public static string animLayerName;

	public static string animStateName;

	public static string animPlaybackRateParam;

	public static float baseDuration;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation(animLayerName, animStateName, animPlaybackRateParam, duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}
}

using RoR2;

namespace EntityStates.Toolbot;

public class ToolbotDualWieldEnd : ToolbotDualWieldBase
{
	public static float baseDuration;

	public static string animLayer;

	public static string animStateName;

	public static string animPlaybackRateParam;

	public static string enterSfx;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation(animLayer, animStateName, animPlaybackRateParam, duration);
		Util.PlaySound(enterSfx, base.gameObject);
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
		return InterruptPriority.Any;
	}
}

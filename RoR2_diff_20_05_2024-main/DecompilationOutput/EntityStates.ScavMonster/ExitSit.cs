using RoR2;

namespace EntityStates.ScavMonster;

public class ExitSit : BaseSitState
{
	public static float baseDuration;

	public static string soundString;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Util.PlaySound(soundString, base.gameObject);
		PlayCrossfade("Body", "ExitSit", "Sit.playbackRate", duration, 0.1f);
		base.modelLocator.normalizeToFloor = false;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}
}

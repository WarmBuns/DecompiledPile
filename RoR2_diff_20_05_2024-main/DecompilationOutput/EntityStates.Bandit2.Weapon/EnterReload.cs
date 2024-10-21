using RoR2;

namespace EntityStates.Bandit2.Weapon;

public class EnterReload : BaseState
{
	public static string enterSoundString;

	public static float baseDuration;

	private float duration => baseDuration / attackSpeedStat;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayCrossfade("Gesture, Additive", "EnterReload", "Reload.playbackRate", duration, 0.1f);
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge > duration)
		{
			outer.SetNextState(new Reload());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}

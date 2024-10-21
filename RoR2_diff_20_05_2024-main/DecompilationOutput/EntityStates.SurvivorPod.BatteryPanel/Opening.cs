using RoR2;

namespace EntityStates.SurvivorPod.BatteryPanel;

public class Opening : BaseBatteryPanelState
{
	public static float duration;

	public static string openSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayPodAnimation("Additive", "OpenPanel", "OpenPanel.playbackRate", duration);
		Util.PlaySound(openSoundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextState(new Opened());
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}
}

namespace EntityStates.SurvivorPod.BatteryPanel;

public class Opened : BaseBatteryPanelState
{
	public override void OnEnter()
	{
		base.OnEnter();
		PlayPodAnimation("Additive", "OpenPanelFinished");
		EnablePickup();
	}
}

namespace EntityStates.Missions.Moon;

public class MoonBatteryInactive : MoonBatteryBaseState
{
	public override void OnEnter()
	{
		base.OnEnter();
		purchaseInteraction.SetAvailable(newAvailable: true);
		FindModelChild("InactiveFX").gameObject.SetActive(value: true);
	}

	public override void OnExit()
	{
		FindModelChild("InactiveFX").gameObject.SetActive(value: false);
		purchaseInteraction.SetAvailable(newAvailable: false);
		base.OnExit();
	}
}

namespace EntityStates.Missions.Moon;

public class MoonBatteryComplete : MoonBatteryBaseState
{
	public override void OnEnter()
	{
		base.OnEnter();
		FindModelChild("ChargedFX").gameObject.SetActive(value: true);
	}
}

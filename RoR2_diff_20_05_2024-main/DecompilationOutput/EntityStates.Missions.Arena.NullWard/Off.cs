namespace EntityStates.Missions.Arena.NullWard;

public class Off : NullWardBaseState
{
	public override void OnEnter()
	{
		base.OnEnter();
		sphereZone.Networkradius = NullWardBaseState.wardRadiusOff;
		purchaseInteraction.SetAvailable(newAvailable: false);
		sphereZone.enabled = false;
	}
}

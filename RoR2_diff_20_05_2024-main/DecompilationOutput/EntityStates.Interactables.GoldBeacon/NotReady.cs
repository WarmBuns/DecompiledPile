namespace EntityStates.Interactables.GoldBeacon;

public class NotReady : GoldBeaconBaseState
{
	public static int count { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		SetReady(ready: false);
		SetPingable(value: true);
		count++;
	}

	public override void OnExit()
	{
		count--;
		base.OnExit();
	}
}

using RoR2;

namespace EntityStates.Missions.Arena.NullWard;

public class WardOnAndReady : NullWardBaseState
{
	public static string soundLoopStartEvent;

	public static string soundLoopEndEvent;

	public override void OnEnter()
	{
		base.OnEnter();
		sphereZone.Networkradius = NullWardBaseState.wardWaitingRadius;
		purchaseInteraction.SetAvailable(newAvailable: true);
		childLocator.FindChild("WardOnEffect").gameObject.SetActive(value: true);
		sphereZone.enabled = true;
		Util.PlaySound(soundLoopStartEvent, base.gameObject);
	}

	public override void OnExit()
	{
		Util.PlaySound(soundLoopEndEvent, base.gameObject);
		base.OnExit();
	}
}

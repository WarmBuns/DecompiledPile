using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Missions.Arena.NullWard;

public class Complete : NullWardBaseState
{
	public static float duration;

	public static string soundEntryEvent;

	public override void OnEnter()
	{
		base.OnEnter();
		sphereZone.Networkradius = NullWardBaseState.wardRadiusOn;
		purchaseInteraction.SetAvailable(newAvailable: false);
		childLocator.FindChild("CompleteEffect").gameObject.SetActive(value: true);
		if (NetworkServer.active)
		{
			base.arenaMissionController.EndRound();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		sphereZone.Networkradius = Mathf.Lerp(NullWardBaseState.wardRadiusOn, NullWardBaseState.wardRadiusOff, base.fixedAge / duration);
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextState(new Off());
		}
	}
}

using RoR2;
using UnityEngine;

namespace EntityStates.SurvivorPod;

public class Landed : SurvivorPodBaseState
{
	private static int IdleStateHash = Animator.StringToHash("Idle");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Base", IdleStateHash);
		Util.PlaySound("Play_UI_podSteamLoop", base.gameObject);
		base.vehicleSeat.handleVehicleExitRequestServer.AddCallback(HandleVehicleExitRequest);
	}

	public override void FixedUpdate()
	{
		if (base.fixedAge > 0f)
		{
			base.survivorPodController.exitAllowed = true;
		}
		base.FixedUpdate();
	}

	private void HandleVehicleExitRequest(GameObject gameObject, ref bool? result)
	{
		base.survivorPodController.exitAllowed = false;
		outer.SetNextState(new PreRelease());
		result = true;
	}

	public override void OnExit()
	{
		base.vehicleSeat.handleVehicleExitRequestServer.RemoveCallback(HandleVehicleExitRequest);
		base.survivorPodController.exitAllowed = false;
		Util.PlaySound("Stop_UI_podSteamLoop", base.gameObject);
	}
}

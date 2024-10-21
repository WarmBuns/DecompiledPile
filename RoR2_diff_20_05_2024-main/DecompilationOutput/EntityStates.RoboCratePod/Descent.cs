using EntityStates.SurvivorPod;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.RoboCratePod;

public class Descent : EntityStates.SurvivorPod.Descent
{
	public static GameObject effectPrefab;

	protected override void TransitionIntoNextState()
	{
		base.TransitionIntoNextState();
		EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, "Base", transmit: true);
	}

	public override void OnExit()
	{
		VehicleSeat component = GetComponent<VehicleSeat>();
		if ((bool)component)
		{
			component.EjectPassenger();
		}
		if (NetworkServer.active)
		{
			EntityState.Destroy(base.gameObject);
		}
		base.OnExit();
	}
}

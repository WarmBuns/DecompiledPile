using EntityStates.SurvivorPod;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.FalseSonMeteorPod;

public class Descent : EntityStates.SurvivorPod.Descent
{
	public static GameObject effectPrefab;

	protected override void TransitionIntoNextState()
	{
		base.TransitionIntoNextState();
	}

	public override void OnExit()
	{
		VehicleSeat component = GetComponent<VehicleSeat>();
		if ((bool)component)
		{
			component.EjectPassenger();
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, "Base", transmit: true);
		}
		if (NetworkServer.active)
		{
			EntityState.Destroy(base.gameObject);
		}
		base.OnExit();
	}
}

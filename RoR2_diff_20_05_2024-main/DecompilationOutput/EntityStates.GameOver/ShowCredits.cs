using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.GameOver;

public class ShowCredits : BaseGameOverControllerState
{
	private GameObject creditsControllerInstance;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			creditsControllerInstance = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/CreditsController"));
			NetworkServer.Spawn(creditsControllerInstance);
		}
	}

	public override void OnExit()
	{
		if (NetworkServer.active && (bool)creditsControllerInstance)
		{
			EntityState.Destroy(creditsControllerInstance);
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && !creditsControllerInstance)
		{
			outer.SetNextState(new ShowReport());
		}
	}
}

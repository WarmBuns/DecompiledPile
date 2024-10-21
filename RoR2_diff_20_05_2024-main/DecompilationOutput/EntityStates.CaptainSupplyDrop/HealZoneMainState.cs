using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.CaptainSupplyDrop;

public class HealZoneMainState : BaseMainState
{
	public static GameObject healZonePrefab;

	private GameObject healZoneInstance;

	protected override Interactability GetInteractability(Interactor activator)
	{
		return Interactability.Disabled;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			healZoneInstance = Object.Instantiate(healZonePrefab, base.transform.position, base.transform.rotation);
			healZoneInstance.GetComponent<TeamFilter>().teamIndex = teamFilter.teamIndex;
			NetworkServer.Spawn(healZoneInstance);
		}
	}

	public override void OnExit()
	{
		if ((bool)healZoneInstance)
		{
			EntityState.Destroy(healZoneInstance);
		}
		base.OnExit();
	}
}

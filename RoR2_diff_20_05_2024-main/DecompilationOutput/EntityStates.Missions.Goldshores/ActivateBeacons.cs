using RoR2;
using UnityEngine.Networking;

namespace EntityStates.Missions.Goldshores;

public class ActivateBeacons : EntityState
{
	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)GoldshoresMissionController.instance)
		{
			GoldshoresMissionController.instance.SpawnBeacons();
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		if (!outer.destroying && (bool)GoldshoresMissionController.instance)
		{
			GoldshoresMissionController.instance.BeginTransitionIntoBossfight();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && (bool)GoldshoresMissionController.instance && GoldshoresMissionController.instance.beaconsActive >= GoldshoresMissionController.instance.beaconsRequiredToSpawnBoss)
		{
			outer.SetNextState(new GoldshoresBossfight());
		}
	}
}

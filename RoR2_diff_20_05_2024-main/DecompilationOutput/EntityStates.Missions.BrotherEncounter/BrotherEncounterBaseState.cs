using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Missions.BrotherEncounter;

public class BrotherEncounterBaseState : EntityState
{
	protected ChildLocator childLocator;

	protected virtual bool shouldEnableArenaWalls => true;

	protected virtual bool shouldEnableArenaNodes => true;

	public override void OnEnter()
	{
		base.OnEnter();
		childLocator = GetComponent<ChildLocator>();
		Transform transform = childLocator.FindChild("ArenaWalls");
		Transform transform2 = childLocator.FindChild("ArenaNodes");
		if ((bool)transform)
		{
			transform.gameObject.SetActive(shouldEnableArenaWalls);
		}
		if ((bool)transform2)
		{
			transform2.gameObject.SetActive(shouldEnableArenaNodes);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public void KillAllMonsters()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		foreach (TeamComponent item in new List<TeamComponent>(TeamComponent.GetTeamMembers(TeamIndex.Monster)))
		{
			if ((bool)item)
			{
				HealthComponent component = item.GetComponent<HealthComponent>();
				if ((bool)component)
				{
					component.Suicide();
				}
			}
		}
	}
}

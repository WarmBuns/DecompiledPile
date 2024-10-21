using System.Collections.ObjectModel;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Engi.EngiWeapon;

public class EngiTeamShield : BaseState
{
	public static float duration = 3f;

	public static float radius;

	public override void OnEnter()
	{
		base.OnEnter();
		if (!base.teamComponent || !NetworkServer.active)
		{
			return;
		}
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(base.teamComponent.teamIndex);
		float num = radius * radius;
		Vector3 position = base.transform.position;
		for (int i = 0; i < teamMembers.Count; i++)
		{
			if (!((teamMembers[i].transform.position - position).sqrMagnitude <= num))
			{
				continue;
			}
			CharacterBody component = teamMembers[i].GetComponent<CharacterBody>();
			if ((bool)component)
			{
				component.AddTimedBuff(JunkContent.Buffs.EngiTeamShield, duration);
				HealthComponent component2 = component.GetComponent<HealthComponent>();
				if ((bool)component2)
				{
					component2.RechargeShieldFull();
				}
			}
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

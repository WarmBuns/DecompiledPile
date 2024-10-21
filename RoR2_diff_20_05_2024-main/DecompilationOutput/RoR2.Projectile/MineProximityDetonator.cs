using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2.Projectile;

public class MineProximityDetonator : MonoBehaviour
{
	public TeamFilter myTeamFilter;

	public UnityEvent triggerEvents;

	public void OnTriggerEnter(Collider collider)
	{
		if (!NetworkServer.active || !collider)
		{
			return;
		}
		HurtBox component = collider.GetComponent<HurtBox>();
		if (!component)
		{
			return;
		}
		HealthComponent healthComponent = component.healthComponent;
		if ((bool)healthComponent)
		{
			TeamComponent component2 = healthComponent.GetComponent<TeamComponent>();
			if (!component2 || component2.teamIndex != myTeamFilter.teamIndex)
			{
				triggerEvents?.Invoke();
			}
		}
	}
}

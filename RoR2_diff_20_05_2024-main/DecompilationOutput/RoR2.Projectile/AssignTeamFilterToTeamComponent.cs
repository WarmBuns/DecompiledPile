using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(HealthComponent))]
public class AssignTeamFilterToTeamComponent : MonoBehaviour
{
	private void Start()
	{
		if (NetworkServer.active)
		{
			TeamComponent component = GetComponent<TeamComponent>();
			TeamFilter component2 = GetComponent<TeamFilter>();
			if ((bool)component2 && (bool)component)
			{
				component.teamIndex = component2.teamIndex;
			}
		}
	}
}

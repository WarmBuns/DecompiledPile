using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(Deployable))]
[RequireComponent(typeof(ProjectileController))]
public class ProjectileDeployToOwner : MonoBehaviour
{
	public DeployableSlot deployableSlot;

	private void Start()
	{
		if (NetworkServer.active)
		{
			DeployToOwner();
		}
	}

	private void DeployToOwner()
	{
		GameObject owner = GetComponent<ProjectileController>().owner;
		if (!owner)
		{
			return;
		}
		CharacterBody component = owner.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			CharacterMaster master = component.master;
			if ((bool)master)
			{
				master.AddDeployable(GetComponent<Deployable>(), deployableSlot);
			}
		}
	}
}

using UnityEngine;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
public class ProjectileImpactEventCaller : MonoBehaviour, IProjectileImpactBehavior
{
	public ProjectileImpactEvent impactEvent;

	public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
	{
		impactEvent?.Invoke(impactInfo);
	}
}

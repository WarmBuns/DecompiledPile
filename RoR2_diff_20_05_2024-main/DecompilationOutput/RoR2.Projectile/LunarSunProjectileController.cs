using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[DisallowMultipleComponent]
[RequireComponent(typeof(ProjectileController))]
[RequireComponent(typeof(ProjectileImpactExplosion))]
[RequireComponent(typeof(ProjectileOwnerOrbiter))]
public class LunarSunProjectileController : MonoBehaviour
{
	private ProjectileImpactExplosion explosion;

	public void OnEnable()
	{
		explosion = GetComponent<ProjectileImpactExplosion>();
		if (NetworkServer.active)
		{
			ProjectileController component = GetComponent<ProjectileController>();
			if ((bool)component.owner)
			{
				AcquireOwner(component);
			}
			else
			{
				component.onInitialized += AcquireOwner;
			}
		}
	}

	private void AcquireOwner(ProjectileController controller)
	{
		controller.onInitialized -= AcquireOwner;
		CharacterBody component = controller.owner.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			ProjectileOwnerOrbiter component2 = GetComponent<ProjectileOwnerOrbiter>();
			component.GetComponent<LunarSunBehavior>().InitializeOrbiter(component2, this);
		}
	}

	public void Detonate()
	{
		if ((bool)explosion)
		{
			explosion.Detonate();
		}
	}
}

using RoR2.Projectile;
using UnityEngine;

public class PrimeDevastatorProjectileController : MonoBehaviour
{
	public ProjectileOwnerOrbiter orbiter;

	public float expansionSpeed = 1f;

	private float orbitExpansionTimer;

	private void Start()
	{
	}

	private void Update()
	{
		ProjectileOwnerOrbiter projectileOwnerOrbiter = orbiter;
		projectileOwnerOrbiter.Networkradius = projectileOwnerOrbiter.radius + Time.deltaTime * expansionSpeed;
	}
}

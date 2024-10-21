using UnityEngine;

namespace RoR2.Projectile;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(ProjectileController))]
[RequireComponent(typeof(ProjectileDamage))]
public class ProjectileDamageTrail : MonoBehaviour
{
	public GameObject trailPrefab;

	public float damageToTrailDpsFactor = 1f;

	public float trailLifetimeAfterExpiration = 1f;

	private ProjectileController projectileController;

	private ProjectileDamage projectileDamage;

	private GameObject currentTrailObject;

	private void Awake()
	{
		projectileController = GetComponent<ProjectileController>();
		projectileDamage = GetComponent<ProjectileDamage>();
	}

	private void FixedUpdate()
	{
		if (!currentTrailObject)
		{
			currentTrailObject = Object.Instantiate(trailPrefab, base.transform.position, base.transform.rotation);
			DamageTrail component = currentTrailObject.GetComponent<DamageTrail>();
			component.damagePerSecond = projectileDamage.damage * damageToTrailDpsFactor;
			component.owner = projectileController.owner;
		}
		else
		{
			currentTrailObject.transform.position = base.transform.position;
		}
	}

	private void OnDestroy()
	{
		DiscontinueTrail();
	}

	private void DiscontinueTrail()
	{
		if ((bool)currentTrailObject)
		{
			currentTrailObject.AddComponent<DestroyOnTimer>().duration = trailLifetimeAfterExpiration;
			currentTrailObject.GetComponent<DamageTrail>().active = false;
			currentTrailObject = null;
		}
	}
}

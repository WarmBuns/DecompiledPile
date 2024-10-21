using UnityEngine;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
[RequireComponent(typeof(ProjectileDamage))]
public class ProjectileMageFirewallController : MonoBehaviour, IProjectileImpactBehavior
{
	public GameObject walkerPrefab;

	private ProjectileController projectileController;

	private ProjectileDamage projectileDamage;

	private bool consumed;

	private void Awake()
	{
		projectileController = GetComponent<ProjectileController>();
		projectileDamage = GetComponent<ProjectileDamage>();
	}

	void IProjectileImpactBehavior.OnProjectileImpact(ProjectileImpactInfo impactInfo)
	{
		if (!consumed)
		{
			consumed = true;
			CreateWalkers();
			Object.Destroy(base.gameObject);
		}
	}

	private void CreateWalkers()
	{
		Vector3 forward = base.transform.forward;
		forward.y = 0f;
		forward.Normalize();
		Vector3 vector = Vector3.Cross(Vector3.up, forward);
		ProjectileManager.instance.FireProjectile(walkerPrefab, base.transform.position + Vector3.up, Util.QuaternionSafeLookRotation(vector), projectileController.owner, projectileDamage.damage, projectileDamage.force, projectileDamage.crit, projectileDamage.damageColorIndex);
		ProjectileManager.instance.FireProjectile(walkerPrefab, base.transform.position + Vector3.up, Util.QuaternionSafeLookRotation(-vector), projectileController.owner, projectileDamage.damage, projectileDamage.force, projectileDamage.crit, projectileDamage.damageColorIndex);
	}
}

using RoR2;
using RoR2.Projectile;
using UnityEngine;

public class RolyPolyBoostedProjectileTimer : MonoBehaviour
{
	private ChefController chefController;

	private void OnEnable()
	{
		ProjectileController component = base.gameObject.GetComponent<ProjectileController>();
		if ((bool)component)
		{
			component.onInitialized += GetChefController;
		}
	}

	private void GetChefController(ProjectileController projectileController)
	{
		chefController = projectileController.owner.GetComponent<ChefController>();
	}

	private void Update()
	{
		if ((bool)chefController && !chefController.rolyPolyActive && Util.HasEffectiveAuthority(base.gameObject))
		{
			Object.Destroy(base.gameObject);
		}
	}
}

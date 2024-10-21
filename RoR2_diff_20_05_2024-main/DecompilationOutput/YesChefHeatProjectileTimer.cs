using System;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

public class YesChefHeatProjectileTimer : MonoBehaviour
{
	private ChefController chefController;

	private ProjectileController projectileController;

	private void OnEnable()
	{
		if (NetworkServer.active)
		{
			projectileController = base.gameObject.GetComponent<ProjectileController>();
			if ((bool)projectileController)
			{
				projectileController.onInitialized += GetChefController;
			}
		}
	}

	private void GetChefController(ProjectileController projectileController)
	{
		chefController = projectileController.owner.GetComponent<ChefController>();
		ChefController obj = chefController;
		obj.yesChefEndedDelegate = (ChefController.YesChefEnded)Delegate.Combine(obj.yesChefEndedDelegate, new ChefController.YesChefEnded(EndChefHeatProjectile));
	}

	private void EndChefHeatProjectile()
	{
		if (NetworkServer.active)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private void OnDestroy()
	{
		if ((bool)projectileController)
		{
			projectileController.onInitialized -= GetChefController;
		}
		if ((bool)chefController)
		{
			ChefController obj = chefController;
			obj.yesChefEndedDelegate = (ChefController.YesChefEnded)Delegate.Remove(obj.yesChefEndedDelegate, new ChefController.YesChefEnded(EndChefHeatProjectile));
		}
	}
}

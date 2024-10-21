using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(HealthComponent))]
[RequireComponent(typeof(ProjectileController))]
[RequireComponent(typeof(ProjectileDamage))]
public class ProjectileGrantOnKillOnDestroy : MonoBehaviour
{
	private ProjectileController projectileController;

	private ProjectileDamage projectileDamage;

	private HealthComponent healthComponent;

	private void OnDestroy()
	{
		healthComponent = GetComponent<HealthComponent>();
		projectileController = GetComponent<ProjectileController>();
		projectileDamage = GetComponent<ProjectileDamage>();
		if (NetworkServer.active && (bool)projectileController.owner)
		{
			DamageInfo damageInfo = new DamageInfo
			{
				attacker = projectileController.owner,
				crit = projectileDamage.crit,
				damage = projectileDamage.damage,
				position = base.transform.position,
				procCoefficient = projectileController.procCoefficient,
				damageType = projectileDamage.damageType,
				damageColorIndex = projectileDamage.damageColorIndex
			};
			HealthComponent victim = healthComponent;
			DamageReport damageReport = new DamageReport(damageInfo, victim, damageInfo.damage, healthComponent.combinedHealth);
			GlobalEventManager.instance.OnCharacterDeath(damageReport);
		}
	}
}

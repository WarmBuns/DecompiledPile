using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
public class ProjectileSingleTargetImpact : MonoBehaviour, IProjectileImpactBehavior
{
	private ProjectileController projectileController;

	private ProjectileDamage projectileDamage;

	private bool alive = true;

	public bool destroyWhenNotAlive = true;

	public bool destroyOnWorld;

	public GameObject impactEffect;

	public string hitSoundString;

	public string enemyHitSoundString;

	private void Awake()
	{
		projectileController = GetComponent<ProjectileController>();
		projectileDamage = GetComponent<ProjectileDamage>();
	}

	public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
	{
		if (!alive)
		{
			return;
		}
		Collider collider = impactInfo.collider;
		if ((bool)collider)
		{
			DamageInfo damageInfo = new DamageInfo();
			if ((bool)projectileDamage)
			{
				damageInfo.damage = projectileDamage.damage;
				damageInfo.crit = projectileDamage.crit;
				damageInfo.attacker = projectileController.owner;
				damageInfo.inflictor = base.gameObject;
				damageInfo.position = impactInfo.estimatedPointOfImpact;
				damageInfo.force = projectileDamage.force * base.transform.forward;
				damageInfo.procChainMask = projectileController.procChainMask;
				damageInfo.procCoefficient = projectileController.procCoefficient;
				damageInfo.damageColorIndex = projectileDamage.damageColorIndex;
				damageInfo.damageType = projectileDamage.damageType;
			}
			HurtBox component = collider.GetComponent<HurtBox>();
			if ((bool)component)
			{
				HealthComponent healthComponent = component.healthComponent;
				if ((bool)healthComponent)
				{
					if (healthComponent.gameObject == projectileController.owner)
					{
						return;
					}
					if (FriendlyFireManager.ShouldDirectHitProceed(healthComponent, projectileController.teamFilter.teamIndex))
					{
						Util.PlaySound(enemyHitSoundString, base.gameObject);
						if (NetworkServer.active)
						{
							damageInfo.ModifyDamageInfo(component.damageModifier);
							healthComponent.TakeDamage(damageInfo);
							GlobalEventManager.instance.OnHitEnemy(damageInfo, component.healthComponent.gameObject);
						}
					}
					alive = false;
				}
			}
			else if (destroyOnWorld)
			{
				alive = false;
			}
			damageInfo.position = base.transform.position;
			if (NetworkServer.active)
			{
				GlobalEventManager.instance.OnHitAll(damageInfo, collider.gameObject);
			}
		}
		if (!alive)
		{
			if (NetworkServer.active && (bool)impactEffect)
			{
				EffectManager.SimpleImpactEffect(impactEffect, impactInfo.estimatedPointOfImpact, -base.transform.forward, !projectileController.isPrediction);
			}
			Util.PlaySound(hitSoundString, base.gameObject);
			if (destroyWhenNotAlive)
			{
				Object.Destroy(base.gameObject);
			}
		}
	}
}

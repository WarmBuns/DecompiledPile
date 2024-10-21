using RoR2.Orbs;
using UnityEngine;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
public class ProjectileHealOwnerOnDamageInflicted : MonoBehaviour, IOnDamageInflictedServerReceiver
{
	public float fractionOfDamage;

	private ProjectileController projectileController;

	private void Awake()
	{
		projectileController = GetComponent<ProjectileController>();
	}

	public void OnDamageInflictedServer(DamageReport damageReport)
	{
		if ((bool)projectileController.owner)
		{
			HealthComponent component = projectileController.owner.GetComponent<HealthComponent>();
			if ((bool)component)
			{
				HealOrb healOrb = new HealOrb();
				healOrb.origin = base.transform.position;
				healOrb.target = component.body.mainHurtBox;
				healOrb.healValue = damageReport.damageDealt * fractionOfDamage;
				healOrb.overrideDuration = 0.3f;
				OrbManager.instance.AddOrb(healOrb);
			}
		}
	}
}

using RoR2.Orbs;
using UnityEngine;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
public class UnseenHandHealingProjectile : MonoBehaviour, IOnDamageInflictedServerReceiver
{
	public bool addBarrier;

	public bool addHealing;

	public float fractionOfDamage;

	public float barrierPercent;

	private int chakraCount;

	public float chakraIncrease;

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
			CharacterBody component2 = projectileController.owner.GetComponent<CharacterBody>();
			if (component2 != null)
			{
				chakraCount = component2.GetBuffCount(DLC2Content.Buffs.ChakraBuff);
			}
			if (addHealing && (bool)component)
			{
				HealOrb healOrb = new HealOrb();
				healOrb.origin = base.transform.position;
				healOrb.target = component.body.mainHurtBox;
				healOrb.healValue = damageReport.damageDealt * (fractionOfDamage + (float)chakraCount * chakraIncrease);
				healOrb.overrideDuration = 0.3f;
				OrbManager.instance.AddOrb(healOrb);
			}
			if (addBarrier)
			{
				component.AddBarrierAuthority(component2.baseMaxHealth * (barrierPercent + (float)chakraCount * chakraIncrease));
			}
		}
	}
}

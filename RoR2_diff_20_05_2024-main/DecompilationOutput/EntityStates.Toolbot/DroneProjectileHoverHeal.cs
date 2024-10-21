using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Toolbot;

public class DroneProjectileHoverHeal : DroneProjectileHover
{
	public static float healPointsCoefficient;

	public static float healFraction;

	protected override void Pulse()
	{
		float num = 1f;
		ProjectileDamage component = GetComponent<ProjectileDamage>();
		if ((bool)component)
		{
			num = component.damage;
		}
		HealOccupants(pulseRadius, healPointsCoefficient * num, healFraction);
		EffectData effectData = new EffectData();
		effectData.origin = base.transform.position;
		effectData.scale = pulseRadius;
		EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/ExplosionVFX"), effectData, transmit: true);
	}

	private static HealthComponent SelectHealthComponent(Collider collider)
	{
		HurtBox component = collider.GetComponent<HurtBox>();
		if ((bool)component && (bool)component.healthComponent)
		{
			return component.healthComponent;
		}
		return null;
	}

	private void HealOccupants(float radius, float healPoints, float healFraction)
	{
		Collider[] colliders;
		int num = HGPhysics.OverlapSphere(out colliders, base.transform.position, radius, LayerIndex.entityPrecise.mask);
		TeamIndex teamIndex = (teamFilter ? teamFilter.teamIndex : TeamIndex.None);
		for (int i = 0; i < num; i++)
		{
			HurtBox component = colliders[i].GetComponent<HurtBox>();
			if (!component || !component.healthComponent)
			{
				continue;
			}
			HealthComponent healthComponent = component.healthComponent;
			if (healthComponent.body.teamComponent.teamIndex == teamIndex)
			{
				float num2 = healPoints + healthComponent.fullHealth * healFraction;
				if (num2 > 0f)
				{
					healthComponent.Heal(num2, default(ProcChainMask));
				}
			}
		}
		HGPhysics.ReturnResults(colliders);
	}
}

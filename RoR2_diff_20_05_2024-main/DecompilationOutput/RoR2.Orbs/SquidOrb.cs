using UnityEngine;

namespace RoR2.Orbs;

public class SquidOrb : GenericDamageOrb
{
	public float forceScalar;

	public override void Begin()
	{
		speed = 120f;
		base.Begin();
	}

	protected override GameObject GetOrbEffect()
	{
		return OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/SquidOrbEffect");
	}

	public override void OnArrival()
	{
		if (!target)
		{
			return;
		}
		HealthComponent healthComponent = target.healthComponent;
		if ((bool)healthComponent)
		{
			Vector3 force = target.transform.position - origin;
			if (force.sqrMagnitude > 0f)
			{
				force.Normalize();
				force *= forceScalar;
			}
			DamageInfo damageInfo = new DamageInfo();
			damageInfo.damage = damageValue;
			damageInfo.attacker = attacker;
			damageInfo.inflictor = null;
			damageInfo.force = Vector3.zero;
			damageInfo.crit = isCrit;
			damageInfo.procChainMask = procChainMask;
			damageInfo.procCoefficient = procCoefficient;
			damageInfo.position = target.transform.position;
			damageInfo.damageColorIndex = damageColorIndex;
			damageInfo.damageType = damageType;
			damageInfo.force = force;
			healthComponent.TakeDamage(damageInfo);
			GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
			GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
		}
	}
}

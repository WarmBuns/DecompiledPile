using UnityEngine;

namespace RoR2.Orbs;

public class GenericDamageOrb : Orb
{
	public float speed = 60f;

	public float damageValue;

	public GameObject attacker;

	public TeamIndex teamIndex;

	public bool isCrit;

	public float scale;

	public ProcChainMask procChainMask;

	public float procCoefficient = 0.2f;

	public DamageColorIndex damageColorIndex;

	public DamageTypeCombo damageType;

	public override void Begin()
	{
		base.duration = base.distanceToTarget / speed;
		if ((bool)GetOrbEffect())
		{
			EffectData effectData = new EffectData
			{
				scale = scale,
				origin = origin,
				genericFloat = base.duration
			};
			effectData.SetHurtBoxReference(target);
			EffectManager.SpawnEffect(GetOrbEffect(), effectData, transmit: true);
		}
	}

	protected virtual GameObject GetOrbEffect()
	{
		return null;
	}

	public override void OnArrival()
	{
		if ((bool)target)
		{
			HealthComponent healthComponent = target.healthComponent;
			if ((bool)healthComponent)
			{
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
				healthComponent.TakeDamage(damageInfo);
				GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
				GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
			}
		}
	}
}

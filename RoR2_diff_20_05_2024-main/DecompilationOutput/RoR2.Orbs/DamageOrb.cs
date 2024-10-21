using UnityEngine;

namespace RoR2.Orbs;

public class DamageOrb : Orb
{
	public enum DamageOrbType
	{
		ClayGooOrb
	}

	private float speed = 60f;

	public float damageValue;

	public GameObject attacker;

	public TeamIndex teamIndex;

	public bool isCrit;

	public ProcChainMask procChainMask;

	public float procCoefficient = 0.2f;

	public DamageColorIndex damageColorIndex;

	public DamageOrbType damageOrbType;

	private DamageTypeCombo orbDamageType = DamageType.Generic;

	public override void Begin()
	{
		GameObject effectPrefab = null;
		if (damageOrbType == DamageOrbType.ClayGooOrb)
		{
			speed = 5f;
			effectPrefab = OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/ClayGooOrbEffect");
			orbDamageType = DamageType.ClayGoo;
		}
		base.duration = base.distanceToTarget / speed;
		EffectData effectData = new EffectData
		{
			origin = origin,
			genericFloat = base.duration
		};
		effectData.SetHurtBoxReference(target);
		EffectManager.SpawnEffect(effectPrefab, effectData, transmit: true);
	}

	public override void OnArrival()
	{
		if (!target)
		{
			return;
		}
		HealthComponent healthComponent = target.healthComponent;
		if (!healthComponent)
		{
			return;
		}
		if (damageOrbType == DamageOrbType.ClayGooOrb)
		{
			CharacterBody component = healthComponent.GetComponent<CharacterBody>();
			if ((bool)component && (component.bodyFlags & CharacterBody.BodyFlags.ImmuneToGoo) != 0)
			{
				healthComponent.Heal(damageValue, default(ProcChainMask));
				return;
			}
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
		damageInfo.damageType = orbDamageType;
		healthComponent.TakeDamage(damageInfo);
		GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
		GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
	}
}

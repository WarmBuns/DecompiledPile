using UnityEngine;

namespace RoR2.Orbs;

public class LunarDetonatorOrb : Orb
{
	public float travelSpeed = 60f;

	public float baseDamage;

	public float damagePerStack;

	public GameObject attacker;

	public bool isCrit;

	public ProcChainMask procChainMask;

	public float procCoefficient;

	public DamageColorIndex damageColorIndex;

	public GameObject detonationEffectPrefab;

	public GameObject orbEffectPrefab;

	public override void Begin()
	{
		base.duration = base.distanceToTarget / travelSpeed;
		EffectData effectData = new EffectData
		{
			scale = 1f,
			origin = origin,
			genericFloat = base.duration
		};
		effectData.SetHurtBoxReference(target);
		if ((bool)orbEffectPrefab)
		{
			EffectManager.SpawnEffect(orbEffectPrefab, effectData, transmit: true);
		}
	}

	public override void OnArrival()
	{
		base.OnArrival();
		if (!target)
		{
			return;
		}
		HealthComponent healthComponent = target.healthComponent;
		if (!healthComponent)
		{
			return;
		}
		CharacterBody body = healthComponent.body;
		if ((bool)body)
		{
			int buffCount = body.GetBuffCount(RoR2Content.Buffs.LunarDetonationCharge);
			if (buffCount > 0)
			{
				body.ClearTimedBuffs(RoR2Content.Buffs.LunarDetonationCharge);
				Vector3 position = target.transform.position;
				DamageInfo damageInfo = new DamageInfo();
				damageInfo.damage = baseDamage + damagePerStack * (float)buffCount;
				damageInfo.attacker = attacker;
				damageInfo.inflictor = null;
				damageInfo.force = Vector3.zero;
				damageInfo.crit = isCrit;
				damageInfo.procChainMask = procChainMask;
				damageInfo.procCoefficient = procCoefficient;
				damageInfo.position = position;
				damageInfo.damageColorIndex = damageColorIndex;
				healthComponent.TakeDamage(damageInfo);
				GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
				GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
				EffectManager.SpawnEffect(detonationEffectPrefab, new EffectData
				{
					origin = position,
					rotation = Quaternion.identity,
					scale = Mathf.Log(buffCount, 5f)
				}, transmit: true);
			}
		}
	}
}

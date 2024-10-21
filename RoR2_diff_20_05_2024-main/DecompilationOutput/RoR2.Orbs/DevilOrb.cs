using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoR2.Orbs;

public class DevilOrb : Orb
{
	public enum EffectType
	{
		Skull,
		Wisp
	}

	private const float speed = 30f;

	public float damageValue;

	public GameObject attacker;

	public TeamIndex teamIndex;

	public bool isCrit;

	public float scale;

	public ProcChainMask procChainMask;

	public float procCoefficient = 0.2f;

	public DamageColorIndex damageColorIndex;

	public EffectType effectType;

	public override void Begin()
	{
		base.duration = base.distanceToTarget / 30f;
		EffectData effectData = new EffectData
		{
			scale = scale,
			origin = origin,
			genericFloat = base.duration
		};
		effectData.SetHurtBoxReference(target);
		GameObject effectPrefab = null;
		switch (effectType)
		{
		case EffectType.Skull:
			effectPrefab = OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/DevilOrbEffect");
			break;
		case EffectType.Wisp:
			effectPrefab = OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/WispOrbEffect");
			break;
		}
		EffectManager.SpawnEffect(effectPrefab, effectData, transmit: true);
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
				healthComponent.TakeDamage(damageInfo);
				GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
				GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
			}
		}
	}

	public HurtBox PickNextTarget(Vector3 position, float range)
	{
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.searchOrigin = position;
		bullseyeSearch.searchDirection = Vector3.zero;
		bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
		bullseyeSearch.teamMaskFilter.RemoveTeam(teamIndex);
		bullseyeSearch.filterByLoS = false;
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
		bullseyeSearch.maxDistanceFilter = range;
		bullseyeSearch.RefreshCandidates();
		List<HurtBox> list = bullseyeSearch.GetResults().ToList();
		if (list.Count <= 0)
		{
			return null;
		}
		return list[Random.Range(0, list.Count)];
	}
}

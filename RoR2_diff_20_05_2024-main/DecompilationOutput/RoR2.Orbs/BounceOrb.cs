using System.Collections.Generic;
using HG;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2.Orbs;

public class BounceOrb : Orb
{
	private const float speed = 70f;

	public float damageValue;

	public GameObject attacker;

	public TeamIndex teamIndex;

	public List<HealthComponent> bouncedObjects;

	public bool isCrit;

	public float scale;

	public ProcChainMask procChainMask;

	public float procCoefficient = 0.2f;

	public DamageColorIndex damageColorIndex;

	public override void Begin()
	{
		base.duration = base.distanceToTarget / 70f;
		EffectData effectData = new EffectData
		{
			scale = scale,
			origin = origin,
			genericFloat = base.duration
		};
		effectData.SetHurtBoxReference(target);
		EffectManager.SpawnEffect(OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/BounceOrbEffect"), effectData, transmit: true);
	}

	public override void OnArrival()
	{
		if ((bool)target)
		{
			HealthComponent healthComponent = target.healthComponent;
			if ((bool)healthComponent)
			{
				Vector3 position = target.transform.position;
				GameObject gameObject = healthComponent.gameObject;
				DamageInfo damageInfo = new DamageInfo();
				damageInfo.damage = damageValue;
				damageInfo.attacker = attacker;
				damageInfo.inflictor = null;
				damageInfo.force = (position - origin).normalized * -1000f;
				damageInfo.crit = isCrit;
				damageInfo.procChainMask = procChainMask;
				damageInfo.procCoefficient = procCoefficient;
				damageInfo.position = position;
				damageInfo.damageColorIndex = damageColorIndex;
				healthComponent.TakeDamage(damageInfo);
				GlobalEventManager.instance.OnHitEnemy(damageInfo, gameObject);
				GlobalEventManager.instance.OnHitAll(damageInfo, gameObject);
			}
		}
	}

	public static void SearchForTargets([NotNull] BullseyeSearch search, TeamIndex teamIndex, Vector3 position, float range, int maxTargets, [NotNull] List<HurtBox> dest, [NotNull] List<HealthComponent> exclusions)
	{
		search.searchOrigin = position;
		search.searchDirection = Vector3.zero;
		search.teamMaskFilter = TeamMask.GetEnemyTeams(teamIndex);
		search.filterByLoS = false;
		search.sortMode = BullseyeSearch.SortMode.None;
		search.maxDistanceFilter = range;
		search.RefreshCandidates();
		List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
		foreach (HurtBox result in search.GetResults())
		{
			list.Add(result);
		}
		Util.ShuffleList(list);
		int i = 0;
		for (int count = list.Count; i < count; i++)
		{
			HurtBox hurtBox = list[i];
			if ((bool)hurtBox.healthComponent && !exclusions.Contains(hurtBox.healthComponent))
			{
				if (exclusions.Count >= maxTargets)
				{
					break;
				}
				exclusions.Add(hurtBox.healthComponent);
				dest.Add(hurtBox);
			}
		}
		CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
	}
}

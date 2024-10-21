using System.Collections.Generic;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.AffixEarthHealer;

public class Heal : BaseState
{
	public static float radius;

	public static float healCoefficient;

	public static float healOrbTravelDuration;

	public static GameObject effectPrefab;

	public override void OnEnter()
	{
		base.OnEnter();
		float healValue = base.characterBody.damage * healCoefficient;
		if (!NetworkServer.active)
		{
			return;
		}
		List<HealthComponent> list = new List<HealthComponent>();
		SphereSearch sphereSearch = new SphereSearch();
		sphereSearch.radius = radius;
		sphereSearch.origin = base.transform.position;
		sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.Ignore;
		sphereSearch.mask = LayerIndex.entityPrecise.mask;
		sphereSearch.RefreshCandidates();
		sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
		HurtBox[] hurtBoxes = sphereSearch.GetHurtBoxes();
		for (int i = 0; i < hurtBoxes.Length; i++)
		{
			HealthComponent item = hurtBoxes[i].healthComponent;
			if (!list.Contains(item))
			{
				list.Add(item);
			}
		}
		foreach (HealthComponent item2 in list)
		{
			HealOrb healOrb = new HealOrb();
			healOrb.origin = base.transform.position;
			healOrb.target = item2.body.mainHurtBox;
			healOrb.healValue = healValue;
			healOrb.overrideDuration = healOrbTravelDuration;
			OrbManager.instance.AddOrb(healOrb);
		}
		EffectManager.SimpleEffect(effectPrefab, base.transform.position, Quaternion.identity, transmit: true);
		base.characterBody.healthComponent.Suicide();
	}
}

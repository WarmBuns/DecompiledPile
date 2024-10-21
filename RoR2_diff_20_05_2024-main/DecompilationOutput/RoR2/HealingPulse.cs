using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class HealingPulse
{
	public static float duration;

	public float healAmount;

	public Vector3 origin;

	public TeamIndex teamIndex;

	public float radius;

	public GameObject effectPrefab;

	public float overShield;

	private readonly List<HealthComponent> healedTargets = new List<HealthComponent>();

	private readonly SphereSearch sphereSearch;

	private TeamMask teamMask;

	private readonly List<HurtBox> hurtBoxesList = new List<HurtBox>();

	public void Fire()
	{
		SphereSearch obj = new SphereSearch
		{
			mask = LayerIndex.entityPrecise.mask,
			origin = origin,
			queryTriggerInteraction = QueryTriggerInteraction.Collide,
			radius = radius
		};
		teamMask = default(TeamMask);
		teamMask.AddTeam(teamIndex);
		obj.RefreshCandidates();
		obj.FilterCandidatesByDistinctHurtBoxEntities();
		obj.RefreshCandidates().FilterCandidatesByHurtBoxTeam(teamMask).FilterCandidatesByDistinctHurtBoxEntities()
			.GetHurtBoxes(hurtBoxesList);
		int i = 0;
		for (int count = hurtBoxesList.Count; i < count; i++)
		{
			HealthComponent healthComponent = hurtBoxesList[i].healthComponent;
			if (!healedTargets.Contains(healthComponent))
			{
				healedTargets.Add(healthComponent);
				HealTarget(healthComponent);
				if (overShield > 0f)
				{
					healthComponent.AddBarrierAuthority(overShield);
				}
			}
		}
		EffectManager.SimpleEffect(effectPrefab, origin, Quaternion.identity, transmit: true);
		hurtBoxesList.Clear();
	}

	private void HealTarget(HealthComponent target)
	{
		target.Heal(healAmount, default(ProcChainMask));
		Util.PlaySound("Play_item_proc_TPhealingNova_hitPlayer", target.gameObject);
	}
}

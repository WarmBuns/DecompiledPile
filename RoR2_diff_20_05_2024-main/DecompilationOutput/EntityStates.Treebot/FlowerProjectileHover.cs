using System.Collections.Generic;
using EntityStates.Toolbot;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Treebot;

public class FlowerProjectileHover : DroneProjectileHover
{
	public static float yankSpeed;

	public static AnimationCurve yankSuitabilityCurve;

	public static float healthFractionYieldPerHit;

	private GameObject owner;

	private ProcChainMask procChainMask;

	private float procCoefficient;

	private float damage;

	private DamageTypeCombo damageType;

	private bool crit;

	private TeamIndex teamIndex = TeamIndex.None;

	private float healPulseHealthFractionValue;

	public override void OnEnter()
	{
		base.OnEnter();
		ProjectileController component = GetComponent<ProjectileController>();
		if ((bool)component)
		{
			owner = component.owner;
			procChainMask = component.procChainMask;
			procCoefficient = component.procCoefficient;
			teamIndex = component.teamFilter.teamIndex;
		}
		ProjectileDamage component2 = GetComponent<ProjectileDamage>();
		if ((bool)component2)
		{
			damage = component2.damage;
			damageType = component2.damageType;
			crit = component2.crit;
		}
	}

	private void FirstPulse()
	{
		Vector3 position = base.transform.position;
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.searchOrigin = position;
		bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(teamIndex);
		bullseyeSearch.maxDistanceFilter = pulseRadius;
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
		bullseyeSearch.filterByLoS = true;
		bullseyeSearch.filterByDistinctEntity = true;
		bullseyeSearch.RefreshCandidates();
		IEnumerable<HurtBox> results = bullseyeSearch.GetResults();
		int num = 0;
		foreach (HurtBox item in results)
		{
			num++;
			Vector3 vector = item.transform.position - position;
			float magnitude = vector.magnitude;
			Vector3 vector2 = vector / magnitude;
			Rigidbody component = item.healthComponent.GetComponent<Rigidbody>();
			float num2 = (component ? component.mass : 1f);
			float num3 = yankSuitabilityCurve.Evaluate(num2);
			Vector3 force = vector2 * (num2 * num3 * (0f - yankSpeed));
			DamageInfo damageInfo = new DamageInfo
			{
				attacker = owner,
				inflictor = base.gameObject,
				crit = crit,
				damage = damage,
				damageColorIndex = DamageColorIndex.Default,
				damageType = damageType,
				force = force,
				position = item.transform.position,
				procChainMask = procChainMask,
				procCoefficient = procCoefficient
			};
			item.healthComponent.TakeDamage(damageInfo);
		}
		healPulseHealthFractionValue = (float)num * healthFractionYieldPerHit / (float)(pulseCount - 1);
	}

	private void HealPulse()
	{
		if ((bool)owner)
		{
			HealthComponent component = owner.GetComponent<HealthComponent>();
			if ((bool)component)
			{
				component.HealFraction(healPulseHealthFractionValue, procChainMask);
			}
		}
	}

	protected override void Pulse()
	{
		if (pulses == 1)
		{
			FirstPulse();
		}
		else
		{
			HealPulse();
		}
	}
}

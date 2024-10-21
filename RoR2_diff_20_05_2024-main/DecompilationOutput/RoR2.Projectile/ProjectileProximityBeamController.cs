using System.Collections.Generic;
using System.Linq;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileController))]
[RequireComponent(typeof(ProjectileDamage))]
public class ProjectileProximityBeamController : MonoBehaviour
{
	private ProjectileController projectileController;

	private ProjectileDamage projectileDamage;

	private List<HealthComponent> previousTargets;

	private TeamFilter teamFilter;

	public int attackFireCount = 1;

	public float attackInterval = 1f;

	public float listClearInterval = 3f;

	public float attackRange = 20f;

	[Range(0f, 180f)]
	public float minAngleFilter;

	[Range(0f, 180f)]
	public float maxAngleFilter = 180f;

	public float procCoefficient = 0.1f;

	public float damageCoefficient = 1f;

	public int bounces;

	public bool inheritDamageType;

	public LightningOrb.LightningType lightningType = LightningOrb.LightningType.BFG;

	private float attackTimer;

	private float listClearTimer;

	private BullseyeSearch search;

	private TeamIndex myTeamIndex
	{
		get
		{
			if (!teamFilter)
			{
				return TeamIndex.Neutral;
			}
			return teamFilter.teamIndex;
		}
	}

	private void Awake()
	{
		if (NetworkServer.active)
		{
			projectileController = GetComponent<ProjectileController>();
			teamFilter = projectileController.teamFilter;
			projectileDamage = GetComponent<ProjectileDamage>();
			attackTimer = 0f;
			previousTargets = new List<HealthComponent>();
			search = new BullseyeSearch();
		}
		else
		{
			base.enabled = false;
		}
	}

	private void ClearList()
	{
		previousTargets.Clear();
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			UpdateServer();
		}
		else
		{
			base.enabled = false;
		}
	}

	private void UpdateServer()
	{
		listClearTimer -= Time.fixedDeltaTime;
		if (listClearTimer <= 0f)
		{
			ClearList();
			listClearTimer = listClearInterval;
		}
		attackTimer -= Time.fixedDeltaTime;
		if (!(attackTimer <= 0f))
		{
			return;
		}
		attackTimer += attackInterval;
		Vector3 position = base.transform.position;
		Vector3 forward = base.transform.forward;
		for (int i = 0; i < attackFireCount; i++)
		{
			HurtBox hurtBox = FindNextTarget(position, forward);
			if ((bool)hurtBox)
			{
				previousTargets.Add(hurtBox.healthComponent);
				LightningOrb lightningOrb = new LightningOrb();
				lightningOrb.bouncedObjects = new List<HealthComponent>();
				lightningOrb.attacker = projectileController.owner;
				lightningOrb.inflictor = base.gameObject;
				lightningOrb.teamIndex = myTeamIndex;
				lightningOrb.damageValue = projectileDamage.damage * damageCoefficient;
				lightningOrb.isCrit = projectileDamage.crit;
				lightningOrb.origin = position;
				lightningOrb.bouncesRemaining = bounces;
				lightningOrb.lightningType = lightningType;
				lightningOrb.procCoefficient = procCoefficient;
				lightningOrb.target = hurtBox;
				lightningOrb.damageColorIndex = projectileDamage.damageColorIndex;
				if (inheritDamageType)
				{
					lightningOrb.damageType = projectileDamage.damageType;
				}
				OrbManager.instance.AddOrb(lightningOrb);
			}
		}
	}

	public HurtBox FindNextTarget(Vector3 position, Vector3 forward)
	{
		search.searchOrigin = position;
		search.searchDirection = forward;
		search.sortMode = BullseyeSearch.SortMode.Distance;
		search.teamMaskFilter = TeamMask.allButNeutral;
		search.teamMaskFilter.RemoveTeam(myTeamIndex);
		search.filterByLoS = false;
		search.minAngleFilter = minAngleFilter;
		search.maxAngleFilter = maxAngleFilter;
		search.maxDistanceFilter = attackRange;
		search.RefreshCandidates();
		return search.GetResults().FirstOrDefault((HurtBox hurtBox) => !previousTargets.Contains(hurtBox.healthComponent));
	}
}

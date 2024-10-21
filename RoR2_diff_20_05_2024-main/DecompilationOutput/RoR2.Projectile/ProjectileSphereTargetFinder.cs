using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace RoR2.Projectile;

[RequireComponent(typeof(ProjectileTargetComponent))]
[RequireComponent(typeof(TeamFilter))]
public class ProjectileSphereTargetFinder : MonoBehaviour
{
	[Tooltip("How far ahead the projectile should look to find a target.")]
	public float lookRange;

	[Tooltip("How long before searching for a target.")]
	public float targetSearchInterval = 0.5f;

	[Tooltip("Will not search for new targets once it has one.")]
	public bool onlySearchIfNoTarget;

	[Tooltip("Allows the target to be lost if it's outside the acceptable range.")]
	public bool allowTargetLoss;

	[Tooltip("If set, targets can only be found when there is a free line of sight.")]
	public bool testLoS;

	[Tooltip("Whether or not airborne characters should be ignored.")]
	public bool ignoreAir;

	[FormerlySerializedAs("altitudeTolerance")]
	[Tooltip("The difference in altitude at which a result will be ignored.")]
	public float flierAltitudeTolerance = float.PositiveInfinity;

	public UnityEvent onNewTargetFound;

	public UnityEvent onTargetLost;

	private new Transform transform;

	private TeamFilter teamFilter;

	private ProjectileTargetComponent targetComponent;

	private float searchTimer;

	private SphereSearch sphereSearch;

	private bool hasTarget;

	private bool hadTargetLastUpdate;

	private HurtBox lastFoundHurtBox;

	private Transform lastFoundTransform;

	private static readonly List<HurtBox> foundHurtBoxes = new List<HurtBox>();

	private void Start()
	{
		if (!NetworkServer.active)
		{
			base.enabled = false;
			return;
		}
		transform = base.transform;
		teamFilter = GetComponent<TeamFilter>();
		targetComponent = GetComponent<ProjectileTargetComponent>();
		sphereSearch = new SphereSearch();
		searchTimer = 0f;
	}

	private void FixedUpdate()
	{
		searchTimer -= Time.fixedDeltaTime;
		if (!(searchTimer <= 0f))
		{
			return;
		}
		searchTimer += targetSearchInterval;
		if (allowTargetLoss && (object)targetComponent.target != null && (object)lastFoundTransform == targetComponent.target && !PassesFilters(lastFoundHurtBox))
		{
			SetTarget(null);
		}
		if (!onlySearchIfNoTarget || targetComponent.target == null)
		{
			SearchForTarget();
		}
		hasTarget = targetComponent.target != null;
		if (hadTargetLastUpdate != hasTarget)
		{
			if (hasTarget)
			{
				onNewTargetFound?.Invoke();
			}
			else
			{
				onTargetLost?.Invoke();
			}
		}
		hadTargetLastUpdate = hasTarget;
	}

	private bool PassesFilters(HurtBox result)
	{
		CharacterBody body = result.healthComponent.body;
		if (!body || (ignoreAir && body.isFlying))
		{
			return false;
		}
		if (body.isFlying && !float.IsInfinity(flierAltitudeTolerance) && flierAltitudeTolerance < Mathf.Abs(result.transform.position.y - transform.position.y))
		{
			return false;
		}
		return true;
	}

	private void SearchForTarget()
	{
		sphereSearch.origin = transform.position;
		sphereSearch.radius = lookRange;
		sphereSearch.mask = LayerIndex.entityPrecise.mask;
		sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
		sphereSearch.RefreshCandidates();
		sphereSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(teamFilter.teamIndex));
		sphereSearch.OrderCandidatesByDistance();
		sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
		sphereSearch.GetHurtBoxes(foundHurtBoxes);
		HurtBox target = null;
		if (foundHurtBoxes.Count > 0)
		{
			int i = 0;
			for (int count = foundHurtBoxes.Count; i < count; i++)
			{
				if (PassesFilters(foundHurtBoxes[i]))
				{
					target = foundHurtBoxes[i];
					break;
				}
			}
			foundHurtBoxes.Clear();
		}
		SetTarget(target);
	}

	private void SetTarget(HurtBox hurtBox)
	{
		lastFoundHurtBox = hurtBox;
		lastFoundTransform = hurtBox?.transform;
		targetComponent.target = lastFoundTransform;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Vector3 position = base.transform.position;
		Gizmos.DrawWireSphere(position, lookRange);
		if (!float.IsInfinity(flierAltitudeTolerance))
		{
			Gizmos.DrawWireCube(position, new Vector3(lookRange * 2f, flierAltitudeTolerance * 2f, lookRange * 2f));
		}
	}
}

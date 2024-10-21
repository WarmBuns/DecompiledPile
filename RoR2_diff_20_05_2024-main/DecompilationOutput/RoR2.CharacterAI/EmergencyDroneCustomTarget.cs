using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.CharacterAI;

[RequireComponent(typeof(BaseAI))]
public class EmergencyDroneCustomTarget : MonoBehaviour
{
	private BaseAI ai;

	private BullseyeSearch search;

	public float searchInterval;

	private float timer;

	private void Awake()
	{
		ai = GetComponent<BaseAI>();
		if (NetworkServer.active)
		{
			search = new BullseyeSearch();
		}
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active)
		{
			FixedUpdateServer();
		}
	}

	private void FixedUpdateServer()
	{
		timer -= Time.fixedDeltaTime;
		if (timer <= 0f)
		{
			timer = searchInterval;
			DoSearch();
		}
	}

	private void DoSearch()
	{
		if ((bool)ai.body)
		{
			Ray aimRay = ai.bodyInputBank.GetAimRay();
			search.viewer = ai.body;
			search.filterByDistinctEntity = true;
			search.filterByLoS = false;
			search.maxDistanceFilter = float.PositiveInfinity;
			search.minDistanceFilter = 0f;
			search.maxAngleFilter = 360f;
			search.searchDirection = aimRay.direction;
			search.searchOrigin = aimRay.origin;
			search.sortMode = BullseyeSearch.SortMode.Distance;
			search.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
			TeamMask none = TeamMask.none;
			none.AddTeam(ai.master.teamIndex);
			search.teamMaskFilter = none;
			search.RefreshCandidates();
			search.FilterOutGameObject(ai.body.gameObject);
			ai.customTarget.gameObject = search.GetResults().Where(TargetPassesFilters).FirstOrDefault()?.healthComponent.gameObject;
		}
	}

	private bool TargetPassesFilters(HurtBox hurtBox)
	{
		if (IsHurt(hurtBox))
		{
			return !HealBeamController.HealBeamAlreadyExists(ai.body.gameObject, hurtBox.healthComponent);
		}
		return false;
	}

	private static bool IsHurt(HurtBox hurtBox)
	{
		if (hurtBox.healthComponent.alive)
		{
			return hurtBox.healthComponent.health < hurtBox.healthComponent.fullHealth;
		}
		return false;
	}
}

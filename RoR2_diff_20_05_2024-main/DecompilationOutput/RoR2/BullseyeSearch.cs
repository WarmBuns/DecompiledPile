using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class BullseyeSearch
{
	private struct CandidateInfo
	{
		public HurtBox hurtBox;

		public Vector3 position;

		public float dot;

		public float distanceSqr;
	}

	public enum SortMode
	{
		None,
		Distance,
		Angle,
		DistanceAndAngle
	}

	private delegate CandidateInfo Selector(HurtBox hurtBox);

	public CharacterBody viewer;

	public Vector3 searchOrigin;

	public Vector3 searchDirection;

	private float minThetaDot = -1f;

	private float maxThetaDot = 1f;

	public float minDistanceFilter;

	public float maxDistanceFilter = float.PositiveInfinity;

	public TeamMask teamMaskFilter = TeamMask.allButNeutral;

	public bool filterByLoS = true;

	public bool filterByDistinctEntity;

	public QueryTriggerInteraction queryTriggerInteraction;

	public SortMode sortMode = SortMode.Distance;

	private List<CandidateInfo> candidatesEnumerable;

	private HashSet<int> DistinctEntityHash = new HashSet<int>();

	public float minAngleFilter
	{
		set
		{
			maxThetaDot = Mathf.Cos(Mathf.Clamp(value, 0f, 180f) * (MathF.PI / 180f));
		}
	}

	public float maxAngleFilter
	{
		set
		{
			minThetaDot = Mathf.Cos(Mathf.Clamp(value, 0f, 180f) * (MathF.PI / 180f));
		}
	}

	private bool filterByDistance
	{
		get
		{
			if (!(minDistanceFilter > 0f) && !(maxDistanceFilter < float.PositiveInfinity))
			{
				if ((bool)viewer)
				{
					return viewer.visionDistance < float.PositiveInfinity;
				}
				return false;
			}
			return true;
		}
	}

	private bool filterByAngle
	{
		get
		{
			if (!(minThetaDot > -1f))
			{
				return maxThetaDot < 1f;
			}
			return true;
		}
	}

	private Func<HurtBox, CandidateInfo> GetSelector()
	{
		bool getDot = filterByAngle;
		bool getDistanceSqr = filterByDistance;
		getDistanceSqr |= sortMode == SortMode.Distance || sortMode == SortMode.DistanceAndAngle;
		getDot |= sortMode == SortMode.Angle || sortMode == SortMode.DistanceAndAngle;
		bool getDifference = getDot || getDistanceSqr;
		bool getPosition = getDot || getDistanceSqr || filterByLoS;
		return delegate(HurtBox hurtBox)
		{
			CandidateInfo candidateInfo = default(CandidateInfo);
			candidateInfo.hurtBox = hurtBox;
			CandidateInfo result = candidateInfo;
			if (getPosition)
			{
				result.position = hurtBox.transform.position;
			}
			Vector3 vector = default(Vector3);
			if (getDifference)
			{
				vector = result.position - searchOrigin;
			}
			if (getDot)
			{
				result.dot = Vector3.Dot(searchDirection, vector.normalized);
			}
			if (getDistanceSqr)
			{
				result.distanceSqr = vector.sqrMagnitude;
			}
			return result;
		};
	}

	public void RefreshCandidates()
	{
		Func<HurtBox, CandidateInfo> selector = GetSelector();
		int count = HurtBox.readOnlyBullseyesList.Count;
		List<CandidateInfo> list = (candidatesEnumerable = new List<CandidateInfo>(count));
		bool flag = filterByAngle;
		bool flag2 = filterByDistance;
		float minDistanceSqr = 0f;
		float maxDistanceSqr = 0f;
		if (flag2)
		{
			float num = maxDistanceFilter;
			if ((bool)viewer)
			{
				num = Mathf.Min(num, viewer.visionDistance);
			}
			minDistanceSqr = minDistanceFilter * minDistanceFilter;
			maxDistanceSqr = num * num;
		}
		for (int i = 0; i < count; i++)
		{
			HurtBox hurtBox = HurtBox.readOnlyBullseyesList[i];
			int item = 0;
			if (filterByDistinctEntity)
			{
				item = hurtBox.healthComponent.GetInstanceID();
				if (DistinctEntityHash.Contains(item))
				{
					continue;
				}
			}
			if (!teamMaskFilter.HasTeam(hurtBox.teamIndex))
			{
				continue;
			}
			CandidateInfo candidateInfo2 = selector(hurtBox);
			if ((!flag || DotOkay(candidateInfo2)) && (!flag2 || DistanceOkay(candidateInfo2)))
			{
				if (filterByDistinctEntity)
				{
					DistinctEntityHash.Add(item);
				}
				list.Add(candidateInfo2);
			}
		}
		Func<CandidateInfo, float> sorter = GetSorter();
		if (sorter != null)
		{
			Comparison<CandidateInfo> comparison = (CandidateInfo x, CandidateInfo y) => sorter(x).CompareTo(sorter(y));
			list.Sort(comparison);
		}
		if (filterByDistinctEntity)
		{
			DistinctEntityHash.Clear();
		}
		bool DistanceOkay(CandidateInfo candidateInfo)
		{
			if (candidateInfo.distanceSqr >= minDistanceSqr)
			{
				return candidateInfo.distanceSqr <= maxDistanceSqr;
			}
			return false;
		}
		bool DotOkay(CandidateInfo candidateInfo)
		{
			if (minThetaDot <= candidateInfo.dot)
			{
				return candidateInfo.dot <= maxThetaDot;
			}
			return false;
		}
	}

	private Func<CandidateInfo, float> GetSorter()
	{
		return sortMode switch
		{
			SortMode.Distance => (CandidateInfo candidateInfo) => candidateInfo.distanceSqr, 
			SortMode.Angle => (CandidateInfo candidateInfo) => 0f - candidateInfo.dot, 
			SortMode.DistanceAndAngle => (CandidateInfo candidateInfo) => (0f - candidateInfo.dot) * candidateInfo.distanceSqr, 
			_ => null, 
		};
	}

	public void FilterCandidatesByHealthFraction(float minHealthFraction = 0f, float maxHealthFraction = 1f)
	{
		if (minHealthFraction > 0f)
		{
			if (maxHealthFraction < 1f)
			{
				candidatesEnumerable.RemoveAll(delegate(CandidateInfo v)
				{
					float combinedHealthFraction = v.hurtBox.healthComponent.combinedHealthFraction;
					return combinedHealthFraction < minHealthFraction || maxHealthFraction < combinedHealthFraction;
				});
			}
			else
			{
				candidatesEnumerable.RemoveAll((CandidateInfo v) => v.hurtBox.healthComponent.combinedHealthFraction < minHealthFraction);
			}
		}
		else if (maxHealthFraction < 1f)
		{
			candidatesEnumerable.RemoveAll((CandidateInfo v) => v.hurtBox.healthComponent.combinedHealthFraction > maxHealthFraction);
		}
	}

	public void FilterOutGameObject(GameObject gameObject)
	{
		candidatesEnumerable.RemoveAll((CandidateInfo v) => v.hurtBox.healthComponent.gameObject == gameObject);
	}

	public IEnumerable<HurtBox> GetResults()
	{
		_ = candidatesEnumerable;
		if (filterByLoS)
		{
			candidatesEnumerable.RemoveAll((CandidateInfo candidateInfo) => !CheckLoS(candidateInfo.position));
		}
		if ((bool)viewer)
		{
			candidatesEnumerable.RemoveAll((CandidateInfo candidateInfo) => !CheckVisible(candidateInfo.hurtBox.healthComponent.gameObject));
		}
		int count = candidatesEnumerable.Count;
		HurtBox[] array = new HurtBox[count];
		for (int i = 0; i < count; i++)
		{
			array[i] = candidatesEnumerable[i].hurtBox;
		}
		return array;
	}

	public bool CheckLoS(Vector3 targetPosition)
	{
		Vector3 direction = targetPosition - searchOrigin;
		RaycastHit hitInfo;
		return !Physics.Raycast(searchOrigin, direction, out hitInfo, direction.magnitude, LayerIndex.world.mask, queryTriggerInteraction);
	}

	public bool CheckVisible(GameObject gameObject)
	{
		CharacterBody component = gameObject.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			return component.GetVisibilityLevel(viewer) >= VisibilityLevel.Revealed;
		}
		return true;
	}

	public void Reset()
	{
		viewer = null;
		searchOrigin = Vector3.zero;
		searchDirection = Vector3.zero;
		minThetaDot = -1f;
		minDistanceFilter = 0f;
		maxDistanceFilter = float.PositiveInfinity;
		teamMaskFilter = TeamMask.allButNeutral;
		filterByLoS = true;
		queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
		sortMode = SortMode.Distance;
	}
}

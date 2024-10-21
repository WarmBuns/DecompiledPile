using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Grumpy.Util;
using UnityEngine;

namespace RoR2;

public class BullseyeSearchOpt
{
	public enum SortMode
	{
		None,
		Distance,
		Angle,
		DistanceAndAngle
	}

	private struct CandidateInfo : IEquatable<CandidateInfo>
	{
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		public struct EntityEqualityComparer : IEqualityComparer<CandidateInfo>
		{
			public bool Equals(CandidateInfo a, CandidateInfo b)
			{
				return (object)a.hurtBox.healthComponent == b.hurtBox.healthComponent;
			}

			public int GetHashCode(CandidateInfo obj)
			{
				return obj.hurtBox.healthComponent.GetHashCode();
			}
		}

		public HurtBox hurtBox;

		public Vector3 position;

		public Vector3 toTarget;

		public float dot;

		public float distanceSqr;

		public bool Equals(CandidateInfo other)
		{
			return hurtBox == other.hurtBox;
		}

		public override bool Equals(object obj)
		{
			if (obj is CandidateInfo)
			{
				return hurtBox == ((CandidateInfo)obj).hurtBox;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return hurtBox.GetHashCode();
		}

		public static bool CandidatesEqual(CandidateInfo a, CandidateInfo b)
		{
			return a.distanceSqr == b.distanceSqr;
		}
	}

	public CharacterBody viewer;

	public Vector3 searchOrigin;

	public Vector3 searchDirection;

	public float minDistanceFilter;

	public float maxDistanceFilter = float.PositiveInfinity;

	public TeamMask teamMaskFilter = TeamMask.allButNeutral;

	public bool filterByLoS = true;

	public bool filterByDistinctEntity;

	public QueryTriggerInteraction queryTriggerInteraction;

	public BullseyeSearch.SortMode sortMode = BullseyeSearch.SortMode.Distance;

	private float minThetaDot = -1f;

	private bool InUse;

	private List<CandidateInfo> candidates = new List<CandidateInfo>(10);

	private Func<CandidateInfo, CandidateInfo, int> CompareDistSqFunc = CompareDistanceSq;

	private Func<CandidateInfo, CandidateInfo, int> CompareDistSqAndAngleFunc = CompareDistSqAndAngle;

	private Func<CandidateInfo, CandidateInfo, int> CompareAngleFunc = CompareAngle;

	private static readonly float fullVisionMinThetaDot = Mathf.Cos(MathF.PI);

	private static BullseyeSearchOpt _search = new BullseyeSearchOpt();

	public float maxAngleFilter
	{
		set
		{
			SetMinAngleDeg(value);
		}
	}

	private bool filterByDistance
	{
		get
		{
			if (!(minDistanceFilter > 0f))
			{
				return maxDistanceFilter < float.PositiveInfinity;
			}
			return true;
		}
	}

	private bool filterByAngle => minThetaDot > fullVisionMinThetaDot;

	public int Count => candidates.Count;

	public void SetMinAngleDeg(float inAngle)
	{
		if (inAngle >= 180f)
		{
			minThetaDot = fullVisionMinThetaDot;
		}
		else
		{
			minThetaDot = Mathf.Cos(inAngle * (MathF.PI / 180f));
		}
	}

	public void RefreshCandidates(bool wantAll = false)
	{
		int count = HurtBox.readOnlyBullseyesList.Count;
		candidates.Clear();
		for (int i = 0; i < count; i++)
		{
			HurtBox hurtBox = HurtBox.readOnlyBullseyesList[i];
			CharacterBody characterBody = null;
			if (!teamMaskFilter.HasTeam(hurtBox.teamIndex))
			{
				continue;
			}
			CandidateInfo item = default(CandidateInfo);
			Vector3 position = hurtBox.transform.position;
			Vector3 vector = position - searchOrigin;
			item.hurtBox = hurtBox;
			item.position = position;
			item.toTarget = vector;
			bool flag = true;
			bool flag2 = true;
			bool flag3 = true;
			float num = minDistanceFilter * minDistanceFilter;
			float num2 = maxDistanceFilter * maxDistanceFilter;
			float num3 = (item.distanceSqr = vector.sqrMagnitude);
			if (filterByDistance)
			{
				flag = num3 >= num && num3 <= num2;
				flag3 = flag3 && flag;
			}
			if (filterByAngle && flag3)
			{
				float num4 = Mathf.Sqrt(num3);
				Vector3 rhs = vector / num4;
				float num5 = Vector3.Dot(searchDirection, rhs);
				flag2 = num5 >= minThetaDot;
				item.dot = num5;
				item.distanceSqr = num3;
				flag3 = flag3 && flag2;
			}
			if (!filterByAngle && flag3 && sortMode == BullseyeSearch.SortMode.Angle)
			{
				float num6 = Mathf.Sqrt(num3);
				Vector3 rhs2 = vector / num6;
				float num7 = Vector3.Dot(searchDirection, rhs2);
				flag2 = num7 >= minThetaDot;
				item.dot = num7;
				item.distanceSqr = num3;
			}
			if (filterByDistinctEntity && flag3)
			{
				for (int j = 0; j < candidates.Count; j++)
				{
					if ((object)candidates[j].hurtBox.healthComponent == hurtBox.healthComponent)
					{
						flag3 = false;
						break;
					}
				}
			}
			if (flag3 && (bool)viewer)
			{
				characterBody = hurtBox.healthComponent.body;
				flag3 &= characterBody.GetVisibilityLevel(viewer) >= VisibilityLevel.Revealed;
			}
			if (wantAll && flag3 && filterByLoS)
			{
				float num8 = Mathf.Sqrt(num3);
				Vector3 direction = vector / num8;
				RaycastHit hitInfo;
				bool flag4 = Physics.Raycast(searchOrigin, direction, out hitInfo, num8, LayerIndex.world.mask, queryTriggerInteraction);
				flag3 = flag3 && !flag4;
			}
			if (flag3)
			{
				candidates.Add(item);
			}
		}
		switch (sortMode)
		{
		case BullseyeSearch.SortMode.Angle:
			HeapSort<CandidateInfo>.Sort(candidates, CompareAngleFunc);
			break;
		case BullseyeSearch.SortMode.Distance:
			HeapSort<CandidateInfo>.Sort(candidates, CompareDistSqFunc);
			break;
		case BullseyeSearch.SortMode.DistanceAndAngle:
			HeapSort<CandidateInfo>.Sort(candidates, CompareDistSqAndAngleFunc);
			break;
		}
	}

	public HurtBox GetClosestCandidate(List<HealthComponent> exclude)
	{
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers((!teamMaskFilter.HasTeam(TeamIndex.Monster)) ? TeamIndex.Player : TeamIndex.Monster);
		int count = teamMembers.Count;
		CandidateInfo candidateInfo = default(CandidateInfo);
		candidateInfo.distanceSqr = float.PositiveInfinity;
		bool flag = false;
		for (int i = 0; i < count; i++)
		{
			CharacterBody body = teamMembers[i].body;
			if (!exclude.Contains(body.healthComponent))
			{
				CandidateInfo candidateInfo2 = default(CandidateInfo);
				Vector3 corePosition = body.corePosition;
				Vector3 vector = corePosition - searchOrigin;
				candidateInfo2.hurtBox = body.mainHurtBox;
				candidateInfo2.position = corePosition;
				bool flag2 = true;
				bool flag3 = true;
				bool flag4 = true;
				float num = minDistanceFilter * minDistanceFilter;
				float num2 = maxDistanceFilter * maxDistanceFilter;
				float num3 = (candidateInfo2.distanceSqr = vector.sqrMagnitude);
				if (filterByDistance)
				{
					flag2 = num3 >= num && num3 <= num2;
					flag4 = flag4 && flag2;
				}
				if (filterByAngle && flag4)
				{
					float num4 = Mathf.Sqrt(num3);
					Vector3 rhs = vector / num4;
					float num5 = Vector3.Dot(searchDirection, rhs);
					flag3 = num5 >= minThetaDot;
					candidateInfo2.dot = num5;
					candidateInfo2.distanceSqr = num3;
					flag4 = flag4 && flag3;
				}
				if (flag4 && (bool)viewer)
				{
					flag4 &= body.GetVisibilityLevel(viewer) >= VisibilityLevel.Revealed;
				}
				if (flag4 && filterByLoS)
				{
					float num6 = Mathf.Sqrt(num3);
					Vector3 direction = vector / num6;
					RaycastHit hitInfo;
					bool flag5 = Physics.Raycast(searchOrigin, direction, out hitInfo, num6, LayerIndex.world.mask, queryTriggerInteraction);
					flag4 = flag4 && !flag5;
				}
				if (flag4 && candidateInfo2.distanceSqr < candidateInfo.distanceSqr)
				{
					candidateInfo = candidateInfo2;
					flag = true;
				}
			}
		}
		if (flag)
		{
			return candidateInfo.hurtBox;
		}
		return null;
	}

	public HurtBox GetClosestCandidateFromList(List<HealthComponent> validTargets, HealthComponent prevTarget, out int resultIndex)
	{
		CandidateInfo candidateInfo = default(CandidateInfo);
		candidateInfo.distanceSqr = float.PositiveInfinity;
		bool flag = false;
		int count = validTargets.Count;
		resultIndex = -1;
		for (int i = 0; i < count; i++)
		{
			HealthComponent healthComponent = validTargets[i];
			if (!healthComponent || !healthComponent.alive || healthComponent == prevTarget)
			{
				continue;
			}
			CharacterBody body = healthComponent.body;
			if ((bool)body && (bool)body.mainHurtBox)
			{
				CandidateInfo candidateInfo2 = default(CandidateInfo);
				Vector3 corePosition = body.corePosition;
				Vector3 vector = corePosition - searchOrigin;
				candidateInfo2.hurtBox = body.mainHurtBox;
				candidateInfo2.position = corePosition;
				bool flag2 = true;
				bool flag3 = true;
				bool flag4 = true;
				if (flag4 && !candidateInfo2.hurtBox.gameObject.activeInHierarchy)
				{
					flag4 = false;
				}
				float num = minDistanceFilter * minDistanceFilter;
				float num2 = maxDistanceFilter * maxDistanceFilter;
				float num3 = (candidateInfo2.distanceSqr = vector.sqrMagnitude);
				if (filterByDistance && flag4)
				{
					flag2 = num3 >= num && num3 <= num2;
					flag4 = flag4 && flag2;
				}
				if (filterByAngle && flag4)
				{
					float num4 = Mathf.Sqrt(num3);
					Vector3 rhs = vector / num4;
					float num5 = Vector3.Dot(searchDirection, rhs);
					flag3 = num5 >= minThetaDot;
					candidateInfo2.dot = num5;
					candidateInfo2.distanceSqr = num3;
					flag4 = flag4 && flag3;
				}
				if (flag4 && (bool)viewer)
				{
					flag4 &= body.GetVisibilityLevel(viewer) >= VisibilityLevel.Revealed;
				}
				if (flag4 && filterByLoS)
				{
					float num6 = Mathf.Sqrt(num3);
					Vector3 direction = vector / num6;
					RaycastHit hitInfo;
					bool flag5 = Physics.Raycast(searchOrigin, direction, out hitInfo, num6, LayerIndex.world.mask, queryTriggerInteraction);
					flag4 = flag4 && !flag5;
				}
				if (flag4 && candidateInfo2.distanceSqr < candidateInfo.distanceSqr)
				{
					candidateInfo = candidateInfo2;
					flag = true;
					resultIndex = i;
				}
			}
		}
		if (flag)
		{
			return candidateInfo.hurtBox;
		}
		return null;
	}

	public void RefreshCandidates_Cheap()
	{
		ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers((!teamMaskFilter.HasTeam(TeamIndex.Monster)) ? TeamIndex.Player : TeamIndex.Monster);
		int count = teamMembers.Count;
		candidates.Clear();
		for (int i = 0; i < count; i++)
		{
			CharacterBody body = teamMembers[i].body;
			CandidateInfo item = default(CandidateInfo);
			Vector3 corePosition = body.corePosition;
			Vector3 vector = corePosition - searchOrigin;
			item.hurtBox = body.mainHurtBox;
			item.position = corePosition;
			item.toTarget = vector;
			bool flag = true;
			bool flag2 = true;
			bool flag3 = true;
			float num = minDistanceFilter * minDistanceFilter;
			float num2 = maxDistanceFilter * maxDistanceFilter;
			float num3 = (item.distanceSqr = vector.sqrMagnitude);
			if (filterByDistance)
			{
				flag = num3 >= num && num3 <= num2;
				flag3 = flag3 && flag;
			}
			if (filterByAngle)
			{
				float num4 = Mathf.Sqrt(num3);
				Vector3 rhs = vector / num4;
				float num5 = Vector3.Dot(searchDirection, rhs);
				flag2 = num5 >= minThetaDot;
				item.dot = num5;
				item.distanceSqr = num3;
				flag3 = flag3 && flag2;
			}
			if (flag3 && (bool)viewer)
			{
				flag3 &= body.GetVisibilityLevel(viewer) >= VisibilityLevel.Revealed;
			}
			if (flag3 && filterByLoS)
			{
				float num6 = Mathf.Sqrt(num3);
				Vector3 direction = vector / num6;
				RaycastHit hitInfo;
				bool flag4 = Physics.Raycast(searchOrigin, direction, out hitInfo, num6, LayerIndex.world.mask, queryTriggerInteraction);
				flag3 = flag3 && !flag4;
			}
			if (flag3)
			{
				candidates.Add(item);
			}
		}
		switch (sortMode)
		{
		case BullseyeSearch.SortMode.Angle:
			HeapSort<CandidateInfo>.Sort(candidates, CompareAngleFunc);
			break;
		case BullseyeSearch.SortMode.Distance:
			HeapSort<CandidateInfo>.Sort(candidates, CompareDistSqFunc);
			break;
		case BullseyeSearch.SortMode.DistanceAndAngle:
			HeapSort<CandidateInfo>.Sort(candidates, CompareDistSqAndAngleFunc);
			break;
		}
	}

	public void FilterOutGameObject(GameObject gameObject)
	{
		for (int num = candidates.Count - 1; num >= 0; num--)
		{
			if (candidates[num].hurtBox.healthComponent.gameObject == gameObject)
			{
				candidates.RemoveAt(num);
			}
		}
	}

	public void FilterToComponent<T>() where T : Component
	{
		for (int num = candidates.Count - 1; num >= 0; num--)
		{
			if (candidates[num].hurtBox.healthComponent.GetComponent<T>() == null)
			{
				candidates.RemoveAt(num);
			}
		}
	}

	public void FilterNotInList(List<HealthComponent> list)
	{
		for (int num = candidates.Count - 1; num >= 0; num--)
		{
			HurtBox hurtBox = candidates[num].hurtBox;
			if (list.Contains(hurtBox.healthComponent))
			{
				candidates.RemoveAt(num);
			}
		}
	}

	public void FilterInList(List<HealthComponent> list)
	{
		for (int num = candidates.Count - 1; num >= 0; num--)
		{
			HurtBox hurtBox = candidates[num].hurtBox;
			if (!list.Contains(hurtBox.healthComponent))
			{
				candidates.RemoveAt(num);
			}
		}
	}

	public HurtBox FirstInList(List<HealthComponent> list)
	{
		HurtBox result = null;
		for (int i = 0; i < candidates.Count; i++)
		{
			HurtBox hurtBox = candidates[i].hurtBox;
			if (!list.Contains(hurtBox.healthComponent))
			{
				result = hurtBox;
				break;
			}
		}
		return result;
	}

	public void FilterCandidatesByHealthFraction(float minHealthFraction = 0f, float maxHealthFraction = 1f)
	{
		if (minHealthFraction > 0f)
		{
			if (maxHealthFraction < 1f)
			{
				for (int num = candidates.Count - 1; num > 0; num--)
				{
					float combinedHealthFraction = candidates[num].hurtBox.healthComponent.combinedHealthFraction;
					if (combinedHealthFraction >= minHealthFraction && !(combinedHealthFraction <= maxHealthFraction))
					{
						candidates.RemoveAt(num);
					}
				}
				return;
			}
			for (int num2 = candidates.Count - 1; num2 > 0; num2--)
			{
				if (!(candidates[num2].hurtBox.healthComponent.combinedHealthFraction >= minHealthFraction))
				{
					candidates.RemoveAt(num2);
				}
			}
		}
		else
		{
			if (!(maxHealthFraction < 1f))
			{
				return;
			}
			for (int num3 = candidates.Count - 1; num3 > 0; num3--)
			{
				if (!(candidates[num3].hurtBox.healthComponent.combinedHealthFraction <= maxHealthFraction))
				{
					candidates.RemoveAt(num3);
				}
			}
		}
	}

	public HurtBox FirstOrDefault()
	{
		HurtBox result = null;
		if (candidates.Count > 0)
		{
			if (filterByLoS)
			{
				for (int i = 0; i < candidates.Count; i++)
				{
					float num = Mathf.Sqrt(candidates[i].distanceSqr);
					Vector3 direction = candidates[i].toTarget / num;
					if (!Physics.Raycast(searchOrigin, direction, out var _, num, LayerIndex.world.mask, queryTriggerInteraction))
					{
						return candidates[i].hurtBox;
					}
				}
			}
			else
			{
				result = candidates[0].hurtBox;
			}
		}
		return result;
	}

	public void DebugPrint()
	{
		for (int i = 0; i < candidates.Count; i++)
		{
			Debug.LogError("C" + i + ": " + candidates[i].hurtBox.healthComponent.gameObject.name + ", " + candidates[i].distanceSqr + ", " + candidates[i].dot);
		}
	}

	public HurtBox FirstOrDefaultNotInList(List<HealthComponent> list)
	{
		HurtBox result = null;
		if (candidates.Count > 0)
		{
			for (int i = 0; i < candidates.Count; i++)
			{
				HurtBox hurtBox = candidates[i].hurtBox;
				if (!list.Contains(hurtBox.healthComponent))
				{
					return hurtBox;
				}
			}
		}
		return result;
	}

	public HurtBox RandomSelect()
	{
		HurtBox result = null;
		if (candidates.Count > 0)
		{
			int index = UnityEngine.Random.Range(0, candidates.Count);
			result = candidates[index].hurtBox;
		}
		return result;
	}

	public void Reset()
	{
		candidates.Clear();
		viewer = null;
		searchOrigin = Vector3.zero;
		searchDirection = Vector3.zero;
		minDistanceFilter = 0f;
		maxDistanceFilter = float.PositiveInfinity;
		teamMaskFilter = TeamMask.allButNeutral;
		filterByLoS = true;
		filterByDistinctEntity = false;
		queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
		sortMode = BullseyeSearch.SortMode.Distance;
		minThetaDot = -1f;
		InUse = false;
	}

	private static float DistSq(Vector3 a, Vector3 b)
	{
		return (a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y) * ((a.z - b.z) * (a.z - b.z));
	}

	private static int CompareDistanceSq(CandidateInfo left, CandidateInfo right)
	{
		float num = left.distanceSqr - right.distanceSqr;
		if (num < 0f)
		{
			return -1;
		}
		if (num > 0f)
		{
			return 1;
		}
		return 0;
	}

	private static int CompareAngle(CandidateInfo left, CandidateInfo right)
	{
		float num = 0f - left.dot;
		float num2 = 0f - right.dot;
		float num3 = num - num2;
		if (num3 < 0f)
		{
			return -1;
		}
		if (num3 > 0f)
		{
			return 1;
		}
		return 0;
	}

	private static int CompareDistSqAndAngle(CandidateInfo left, CandidateInfo right)
	{
		float num = (0f - left.dot) * left.distanceSqr;
		float num2 = (0f - right.dot) * right.distanceSqr;
		float num3 = num - num2;
		if (num3 < 0f)
		{
			return -1;
		}
		if (num3 > 0f)
		{
			return 1;
		}
		return 0;
	}

	public HurtBox GetHurtBox(int idx)
	{
		return candidates[idx].hurtBox;
	}

	public static BullseyeSearchOpt FromPool()
	{
		if (!_search.InUse)
		{
			_search.InUse = true;
		}
		else
		{
			Debug.LogErrorFormat("BullseyeSearch at limit");
		}
		return _search;
	}

	public static void ReturnPool(BullseyeSearchOpt inSearch)
	{
		if (_search == inSearch)
		{
			_search.Reset();
		}
		else
		{
			Debug.LogError("BullseyeSearch in use!");
		}
	}
}

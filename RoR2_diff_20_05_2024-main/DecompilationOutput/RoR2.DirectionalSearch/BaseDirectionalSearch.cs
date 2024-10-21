using System;
using System.Collections.Generic;
using HG;
using UnityEngine;

namespace RoR2.DirectionalSearch;

public class BaseDirectionalSearch<TSource, TSelector, TCandidateFilter> where TSource : class where TSelector : IGenericWorldSearchSelector<TSource> where TCandidateFilter : IGenericDirectionalSearchFilter<TSource>
{
	private struct CandidateInfo
	{
		public TSource source;

		public Vector3 position;

		public Vector3 diff;

		public float distance;

		public float dot;

		public GameObject entity;
	}

	public Vector3 searchOrigin;

	public Vector3 searchDirection;

	private float minThetaDot = -1f;

	private float maxThetaDot = 1f;

	public float minDistanceFilter;

	public float maxDistanceFilter = float.PositiveInfinity;

	public SortMode sortMode = SortMode.Distance;

	public bool filterByLoS = true;

	public bool filterByDistinctEntity;

	private CandidateInfo[] candidateInfoList = Array.Empty<CandidateInfo>();

	private int candidateCount;

	protected TSelector selector;

	protected TCandidateFilter candidateFilter;

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

	public BaseDirectionalSearch(TSelector selector, TCandidateFilter candidateFilter)
	{
		this.selector = selector;
		this.candidateFilter = candidateFilter;
	}

	public TSource SearchCandidatesForSingleTarget<TSourceEnumerable>(TSourceEnumerable sourceEnumerable) where TSourceEnumerable : IEnumerable<TSource>
	{
		ArrayUtils.Clear(candidateInfoList, ref candidateCount);
		float num = minDistanceFilter * minDistanceFilter;
		float num2 = maxDistanceFilter * maxDistanceFilter;
		foreach (TSource item in sourceEnumerable)
		{
			CandidateInfo candidateInfo = default(CandidateInfo);
			candidateInfo.source = item;
			CandidateInfo value = candidateInfo;
			Transform transform = selector.GetTransform(item);
			if (!transform)
			{
				continue;
			}
			value.position = transform.position;
			value.diff = value.position - searchOrigin;
			float sqrMagnitude = value.diff.sqrMagnitude;
			if (!(sqrMagnitude < num) && !(sqrMagnitude > num2))
			{
				value.distance = Mathf.Sqrt(sqrMagnitude);
				value.dot = ((value.distance == 0f) ? 0f : Vector3.Dot(searchDirection, value.diff / value.distance));
				if (!(value.dot < minThetaDot) && !(value.dot > maxThetaDot))
				{
					value.entity = selector.GetRootObject(item);
					ArrayUtils.ArrayAppend(ref candidateInfoList, ref candidateCount, in value);
				}
			}
		}
		for (int num3 = candidateCount - 1; num3 >= 0; num3--)
		{
			if (!candidateFilter.PassesFilter(candidateInfoList[num3].source))
			{
				ArrayUtils.ArrayRemoveAt(candidateInfoList, ref candidateCount, num3);
			}
		}
		Array.Sort(candidateInfoList, GetSorter());
		if (filterByDistinctEntity)
		{
			for (int num4 = candidateCount - 1; num4 >= 0; num4--)
			{
				ref CandidateInfo reference = ref candidateInfoList[num4];
				for (int i = 0; i < num4; i++)
				{
					ref CandidateInfo reference2 = ref candidateInfoList[i];
					if (reference.entity == reference2.entity)
					{
						ArrayUtils.ArrayRemoveAt(candidateInfoList, ref candidateCount, num4);
						break;
					}
				}
			}
		}
		TSource result = null;
		if (filterByLoS)
		{
			for (int j = 0; j < candidateCount; j++)
			{
				if (!Physics.Linecast(searchOrigin, candidateInfoList[j].position, out var _, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
				{
					result = candidateInfoList[j].source;
					break;
				}
			}
		}
		else if (candidateCount > 0)
		{
			result = candidateInfoList[0].source;
		}
		ArrayUtils.Clear(candidateInfoList, ref candidateCount);
		return result;
	}

	private static int DistanceToInversePriority(CandidateInfo a, CandidateInfo b)
	{
		return a.distance.CompareTo(b.distance);
	}

	private static int AngleToInversePriority(CandidateInfo a, CandidateInfo b)
	{
		return (0f - a.dot).CompareTo(0f - b.dot);
	}

	private static int DistanceAndAngleToInversePriority(CandidateInfo a, CandidateInfo b)
	{
		return ((0f - a.dot) * a.distance).CompareTo((0f - b.dot) * b.distance);
	}

	private Comparison<CandidateInfo> GetSorter()
	{
		return sortMode switch
		{
			SortMode.Distance => DistanceToInversePriority, 
			SortMode.Angle => AngleToInversePriority, 
			SortMode.DistanceAndAngle => DistanceAndAngleToInversePriority, 
			_ => null, 
		};
	}
}

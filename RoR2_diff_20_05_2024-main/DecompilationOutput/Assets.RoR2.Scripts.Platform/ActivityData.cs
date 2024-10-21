using System.Collections.Generic;
using UnityEngine;

namespace Assets.RoR2.Scripts.Platform;

[CreateAssetMenu(menuName = "RoR2/ActivityData")]
public class ActivityData : ScriptableObject
{
	[SerializeField]
	private List<BaseActivity> activities;

	public BaseActivity LookupActivityByID(string activityID)
	{
		foreach (BaseActivity activity in activities)
		{
			if (activity.ActivityID == activityID)
			{
				return activity;
			}
		}
		return null;
	}

	public List<BaseActivity> CompareActivityCriteria(BaseActivitySelector activitySelector)
	{
		List<BaseActivity> list = new List<BaseActivity>();
		foreach (BaseActivity activity in activities)
		{
			if (activitySelector.IsCompatibleWith(activity))
			{
				list.Add(activity);
			}
		}
		return list;
	}
}

using System.Collections.Generic;
using UnityEngine;

public class JobCleaner : MonoBehaviour
{
	public static JobCleaner instance;

	public List<JobWrapper> jobsToClean;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		jobsToClean = new List<JobWrapper>();
	}

	private void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
		}
		if (jobsToClean.Count <= 0)
		{
			return;
		}
		Debug.LogError("JOBCLEANER HAS JOBS TO CLEAN BEFORE BEING DESTROYED");
		while (jobsToClean.Count > 0)
		{
			for (int num = jobsToClean.Count - 1; num >= 0; num--)
			{
				if (jobsToClean[num].handle.IsCompleted)
				{
					jobsToClean[num].handle.Complete();
					jobsToClean[num].Dispose();
					jobsToClean.RemoveAt(num);
				}
			}
		}
		Debug.LogError("JOBCLEANER FINISHED CLEANING JOBS, CAN DIE HAPPY NOW");
	}

	private void LateUpdate()
	{
		if (jobsToClean.Count > 0)
		{
			int index = jobsToClean.Count - 1;
			if (jobsToClean[index].handle.IsCompleted)
			{
				jobsToClean[index].handle.Complete();
				jobsToClean[index].Dispose();
				jobsToClean.RemoveAt(index);
			}
		}
	}
}

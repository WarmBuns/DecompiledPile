using Unity.Jobs;
using UnityEngine;

public class JobWrapper
{
	public JobHandle handle;

	public bool jobScheduled;

	public bool isComplete
	{
		get
		{
			if (jobScheduled)
			{
				return handle.IsCompleted;
			}
			return true;
		}
	}

	public virtual void Dispose()
	{
		Debug.LogError("Error: No overload for JobWrapper.Dispose()");
	}

	public virtual void Initialize(Object data)
	{
		Debug.LogError("Error: No overload for JobWrapper:Initialize()");
	}

	public virtual void Schedule()
	{
		JobHandle.ScheduleBatchedJobs();
		jobScheduled = true;
	}

	public void Complete()
	{
		handle.Complete();
		jobScheduled = false;
	}

	public bool SendToCleaner()
	{
		if (JobCleaner.instance != null)
		{
			JobCleaner.instance.jobsToClean.Add(this);
			return true;
		}
		return false;
	}
}

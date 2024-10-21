using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public static class WorkQueue
{
	private static Queue<Func<bool>> queuedWork;

	private static int workPerFrame;

	private static int workPerFrame_Max;

	private static int queuedBacklogMinimum;

	private static int workPerFrame_BacklogMax;

	private static bool useFlexBudgeting;

	private static bool useQueueing;

	private static ArrayAverage performanceAverager;

	static WorkQueue()
	{
		queuedWork = new Queue<Func<bool>>(150);
		workPerFrame_Max = 25;
		queuedBacklogMinimum = 500;
		workPerFrame_BacklogMax = 250;
		useFlexBudgeting = true;
		useQueueing = true;
		performanceAverager = new ArrayAverage(100);
	}

	public static void RequestUpdate()
	{
		if (Time.deltaTime <= 0f)
		{
			return;
		}
		workPerFrame = 0;
		int num = Mathf.Min(queuedWork.Count);
		int num2 = ((useFlexBudgeting && queuedWork.Count > queuedBacklogMinimum) ? workPerFrame_BacklogMax : workPerFrame_Max);
		if (useQueueing)
		{
			for (int i = 0; i < num; i++)
			{
				if (queuedWork.Dequeue()())
				{
					workPerFrame++;
					if (workPerFrame >= num2)
					{
						break;
					}
				}
			}
		}
		else
		{
			num2 = 1000000;
			while (queuedWork.Count > 0)
			{
				if (queuedWork.Dequeue()())
				{
					workPerFrame++;
				}
			}
		}
		if (workPerFrame > 0)
		{
			performanceAverager.nextValue = workPerFrame;
			_ = useQueueing;
		}
	}

	public static void ExecuteOrEnqueue(Func<bool> spawnCallback)
	{
		if (CanAffordProjectile())
		{
			workPerFrame++;
			spawnCallback();
		}
		else
		{
			queuedWork.Enqueue(spawnCallback);
		}
	}

	private static bool CanAffordProjectile()
	{
		return workPerFrame < workPerFrame_Max;
	}
}

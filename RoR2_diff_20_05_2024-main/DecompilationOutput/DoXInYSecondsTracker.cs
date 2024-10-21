using System;
using HG;

public class DoXInYSecondsTracker
{
	private readonly float[] timestamps;

	private readonly float window;

	private int lastValidCount;

	private float newestTime => timestamps[0];

	private int requirement => timestamps.Length;

	public DoXInYSecondsTracker(int requirement, float window)
	{
		if (requirement < 1)
		{
			throw new ArgumentException("Argument must be greater than zero", "requirement");
		}
		timestamps = new float[requirement];
		Clear();
		this.window = window;
	}

	public void Clear()
	{
		for (int i = 0; i < timestamps.Length; i++)
		{
			timestamps[i] = float.NegativeInfinity;
		}
	}

	private int FindInsertionPosition(float t)
	{
		for (int i = 0; i < lastValidCount; i++)
		{
			if (timestamps[i] < t)
			{
				return i;
			}
		}
		return lastValidCount;
	}

	public bool Push(float t)
	{
		float num = t - window;
		if (t < newestTime)
		{
			lastValidCount = timestamps.Length;
		}
		int num2 = lastValidCount - 1;
		while (num2 >= 0 && !(num <= timestamps[num2]))
		{
			lastValidCount--;
			num2--;
		}
		int num3 = FindInsertionPosition(t);
		if (num3 < timestamps.Length)
		{
			lastValidCount++;
			ArrayUtils.ArrayInsertNoResize(timestamps, lastValidCount, num3, in t);
		}
		return lastValidCount == requirement;
	}
}

using UnityEngine;

namespace RoR2;

public class ArrayAverage
{
	private int[] valueArray;

	private int arrayIndex;

	public float peakAverage;

	public float peakSingleValue;

	public float average
	{
		get
		{
			float num = 0f;
			float num2 = 0f;
			for (int i = 0; i < valueArray.Length; i++)
			{
				int num3 = valueArray[i];
				if (num3 != 0)
				{
					num += (float)num3;
					num2 += 1f;
				}
			}
			if (num2 == 0f)
			{
				return 0f;
			}
			num /= num2;
			if (num > peakAverage)
			{
				peakAverage = num;
			}
			return num;
		}
	}

	public int nextValue
	{
		get
		{
			return valueArray[arrayIndex];
		}
		set
		{
			if ((float)value > peakSingleValue)
			{
				peakSingleValue = value;
			}
			arrayIndex = (arrayIndex + 1) % valueArray.Length;
			valueArray[arrayIndex] = value;
		}
	}

	public ArrayAverage(int arraySize)
	{
		arrayIndex = 0;
		valueArray = new int[Mathf.Max(arraySize, 1)];
	}
}

using System;
using UnityEngine;

public class WeightedSelection<T>
{
	[Serializable]
	public struct ChoiceInfo
	{
		public T value;

		public float weight;
	}

	[HideInInspector]
	[SerializeField]
	public ChoiceInfo[] choices;

	[HideInInspector]
	[SerializeField]
	private int _count;

	[HideInInspector]
	[SerializeField]
	private float totalWeight;

	private const int minCapacity = 8;

	public int Count
	{
		get
		{
			return _count;
		}
		private set
		{
			_count = value;
		}
	}

	public int Capacity
	{
		get
		{
			return choices.Length;
		}
		set
		{
			if (value < 8 || value < Count)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			ChoiceInfo[] sourceArray = choices;
			choices = new ChoiceInfo[value];
			Array.Copy(sourceArray, choices, Count);
		}
	}

	public WeightedSelection(int capacity = 8)
	{
		choices = new ChoiceInfo[capacity];
	}

	public void AddChoice(T value, float weight)
	{
		AddChoice(new ChoiceInfo
		{
			value = value,
			weight = weight
		});
	}

	public void AddChoice(ChoiceInfo choice)
	{
		if (Count == Capacity)
		{
			Capacity *= 2;
		}
		choices[Count++] = choice;
		totalWeight += choice.weight;
	}

	public void RemoveChoice(int choiceIndex)
	{
		if (choiceIndex < 0 || Count <= choiceIndex)
		{
			throw new ArgumentOutOfRangeException("choiceIndex");
		}
		int i = choiceIndex;
		for (int num = Count - 1; i < num; i++)
		{
			choices[i] = choices[i + 1];
		}
		choices[--Count] = default(ChoiceInfo);
		RecalculateTotalWeight();
	}

	public void ModifyChoiceWeight(int choiceIndex, float newWeight)
	{
		choices[choiceIndex].weight = newWeight;
		RecalculateTotalWeight();
	}

	public void Clear()
	{
		for (int i = 0; i < Count; i++)
		{
			choices[i] = default(ChoiceInfo);
		}
		Count = 0;
		totalWeight = 0f;
	}

	private void RecalculateTotalWeight()
	{
		totalWeight = 0f;
		for (int i = 0; i < Count; i++)
		{
			totalWeight += choices[i].weight;
		}
	}

	public T Evaluate(float normalizedIndex)
	{
		int num = EvaluateToChoiceIndex(normalizedIndex);
		return choices[num].value;
	}

	public int EvaluateToChoiceIndex(float normalizedIndex)
	{
		return EvaluateToChoiceIndex(normalizedIndex, null);
	}

	public int EvaluateToChoiceIndex(float normalizedIndex, int[] ignoreIndices)
	{
		if (Count == 0)
		{
			throw new InvalidOperationException("Cannot call Evaluate without available choices.");
		}
		float num = totalWeight;
		if (ignoreIndices != null)
		{
			foreach (int num2 in ignoreIndices)
			{
				num -= choices[num2].weight;
			}
		}
		float num3 = normalizedIndex * num;
		float num4 = 0f;
		for (int j = 0; j < Count; j++)
		{
			if (ignoreIndices == null || Array.IndexOf(ignoreIndices, j) == -1)
			{
				num4 += choices[j].weight;
				if (num3 < num4)
				{
					return j;
				}
			}
		}
		return Count - 1;
	}

	public ChoiceInfo GetChoice(int i)
	{
		return choices[i];
	}
}

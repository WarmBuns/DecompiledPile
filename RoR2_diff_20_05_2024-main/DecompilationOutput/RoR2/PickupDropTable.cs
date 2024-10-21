using System;
using System.Collections.Generic;
using RoR2.Items;
using UnityEngine;

namespace RoR2;

public abstract class PickupDropTable : ScriptableObject
{
	public bool canDropBeReplaced = true;

	private static readonly List<PickupDropTable> instancesList;

	protected static PickupIndex[] GenerateUniqueDropsFromWeightedSelection(int maxDrops, Xoroshiro128Plus rng, WeightedSelection<PickupIndex> weightedSelection)
	{
		int num = Math.Min(maxDrops, weightedSelection.Count);
		int[] array = Array.Empty<int>();
		PickupIndex[] array2 = new PickupIndex[num];
		for (int i = 0; i < num; i++)
		{
			int num2 = weightedSelection.EvaluateToChoiceIndex(rng.nextNormalizedFloat, array);
			array2[i] = weightedSelection.GetChoice(num2).value;
			Array.Resize(ref array, i + 1);
			array[i] = num2;
		}
		return array2;
	}

	protected static PickupIndex GenerateDropFromWeightedSelection(Xoroshiro128Plus rng, WeightedSelection<PickupIndex> weightedSelection)
	{
		if (weightedSelection.Count > 0)
		{
			return weightedSelection.Evaluate(rng.nextNormalizedFloat);
		}
		return PickupIndex.none;
	}

	public abstract int GetPickupCount();

	protected abstract PickupIndex GenerateDropPreReplacement(Xoroshiro128Plus rng);

	public PickupIndex GenerateDrop(Xoroshiro128Plus rng)
	{
		PickupIndex pickupIndex = GenerateDropPreReplacement(rng);
		if (pickupIndex == PickupIndex.none)
		{
			Debug.LogError("Could not generate pickup index from droptable.");
		}
		if (!pickupIndex.isValid)
		{
			Debug.LogError("Pickup index from droptable is invalid.");
		}
		if (canDropBeReplaced)
		{
			return RandomlyLunarUtils.CheckForLunarReplacement(pickupIndex, rng);
		}
		return pickupIndex;
	}

	protected abstract PickupIndex[] GenerateUniqueDropsPreReplacement(int maxDrops, Xoroshiro128Plus rng);

	public PickupIndex[] GenerateUniqueDrops(int maxDrops, Xoroshiro128Plus rng)
	{
		PickupIndex[] array = GenerateUniqueDropsPreReplacement(maxDrops, rng);
		if (canDropBeReplaced)
		{
			RandomlyLunarUtils.CheckForLunarReplacementUniqueArray(array, rng);
		}
		return array;
	}

	protected virtual void Regenerate(Run run)
	{
	}

	protected virtual void OnEnable()
	{
		instancesList.Add(this);
		if ((bool)Run.instance)
		{
			Regenerate(Run.instance);
		}
	}

	protected virtual void OnDisable()
	{
		instancesList.Remove(this);
	}

	static PickupDropTable()
	{
		instancesList = new List<PickupDropTable>();
		Run.onRunStartGlobal += RegenerateAll;
		Run.onAvailablePickupsModified += RegenerateAll;
	}

	private static void RegenerateAll(Run run)
	{
		for (int i = 0; i < instancesList.Count; i++)
		{
			instancesList[i].Regenerate(run);
		}
	}

	public void ModifyTierWeights(float tier1, float tier2, float tier3)
	{
	}
}

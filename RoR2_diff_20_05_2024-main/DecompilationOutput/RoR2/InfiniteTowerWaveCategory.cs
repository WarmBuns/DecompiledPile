using System;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/Infinite Tower/Infinite Tower Wave Category")]
public class InfiniteTowerWaveCategory : ScriptableObject
{
	[Serializable]
	public struct WeightedWave
	{
		public GameObject wavePrefab;

		public InfiniteTowerWavePrerequisites prerequisites;

		public float weight;
	}

	private readonly WeightedSelection<GameObject> weightedSelection = new WeightedSelection<GameObject>();

	[SerializeField]
	[Tooltip("The weighted selection for which wave prefab to use")]
	private WeightedWave[] wavePrefabs;

	[SerializeField]
	[Tooltip("The period of how often this category is available, e.g.\"every N waves\".")]
	private int availabilityPeriod;

	[SerializeField]
	[Tooltip("The minimum wave index where this category is available (inclusive)")]
	private int minWaveIndex;

	public bool IsAvailable(InfiniteTowerRun run)
	{
		bool num = availabilityPeriod > 0 && run.waveIndex % availabilityPeriod == 0;
		bool flag = run.waveIndex >= minWaveIndex;
		bool flag2 = wavePrefabs.Length != 0;
		if (num && flag && flag2)
		{
			WeightedWave[] array = wavePrefabs;
			for (int i = 0; i < array.Length; i++)
			{
				WeightedWave weightedWave = array[i];
				if (!weightedWave.prerequisites || weightedWave.prerequisites.AreMet(run))
				{
					return true;
				}
			}
		}
		return false;
	}

	public GameObject SelectWavePrefab(InfiniteTowerRun run, Xoroshiro128Plus rng)
	{
		if (weightedSelection.Count < wavePrefabs.Length)
		{
			GenerateWeightedSelection();
		}
		WeightedWave[] array = wavePrefabs;
		for (int i = 0; i < array.Length; i++)
		{
			WeightedWave weightedWave = array[i];
			if (!weightedWave.prerequisites || weightedWave.prerequisites.AreMet(run))
			{
				continue;
			}
			for (int j = 0; j < weightedSelection.Count; j++)
			{
				if ((object)weightedSelection.choices[j].value == weightedWave.wavePrefab)
				{
					weightedSelection.RemoveChoice(j);
					break;
				}
			}
		}
		return weightedSelection.Evaluate(rng.nextNormalizedFloat);
	}

	private void GenerateWeightedSelection()
	{
		weightedSelection.Clear();
		WeightedWave[] array = wavePrefabs;
		for (int i = 0; i < array.Length; i++)
		{
			WeightedWave weightedWave = array[i];
			weightedSelection.AddChoice(weightedWave.wavePrefab, weightedWave.weight);
		}
	}
}

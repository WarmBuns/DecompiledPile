using System;
using System.Linq;
using RoR2.ExpansionManagement;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/DCCS/DccsPool")]
public class DccsPool : ScriptableObject
{
	[Serializable]
	public class PoolEntry : ISerializationCallbackReceiver
	{
		public DirectorCardCategorySelection dccs;

		[Tooltip("The weight of this entry relative to its siblings")]
		public float weight;

		[HideInInspector]
		[SerializeField]
		protected bool hasBeenSerialized;

		public void OnBeforeSerialize()
		{
			hasBeenSerialized = true;
		}

		public virtual void OnAfterDeserialize()
		{
			if (!hasBeenSerialized)
			{
				weight = 1f;
			}
		}
	}

	[Serializable]
	public class ConditionalPoolEntry : PoolEntry
	{
		[Tooltip("ALL expansions in this list must be enabled for this run for this entry to be considered.")]
		public ExpansionDef[] requiredExpansions;
	}

	[Serializable]
	public class Category : ISerializationCallbackReceiver
	{
		[Tooltip("A name to help identify this category")]
		public string name;

		[Tooltip("The weight of all entries in this category relative to the sibling categories.")]
		public float categoryWeight = 1f;

		[Tooltip("These entries are always considered.")]
		public PoolEntry[] alwaysIncluded;

		[Tooltip("These entries are only considered if their individual conditions are met.")]
		public ConditionalPoolEntry[] includedIfConditionsMet;

		[Tooltip("These entries are considered only if no entries from 'includedIfConditionsMet' have been included.")]
		public PoolEntry[] includedIfNoConditionsMet;

		[HideInInspector]
		[SerializeField]
		protected bool hasBeenSerialized;

		public void OnBeforeSerialize()
		{
			hasBeenSerialized = true;
		}

		public void OnAfterDeserialize()
		{
			if (!hasBeenSerialized)
			{
				categoryWeight = 1f;
			}
		}
	}

	[SerializeField]
	private Category[] poolCategories;

	internal DirectorCardCategorySelection TrySelectPool(string selectionName)
	{
		return poolCategories.FirstOrDefault((Category category) => category.includedIfConditionsMet.Any((ConditionalPoolEntry entry) => entry.dccs.name == selectionName))?.includedIfConditionsMet.First().dccs;
	}

	public WeightedSelection<DirectorCardCategorySelection> GenerateWeightedSelection()
	{
		WeightedSelection<DirectorCardCategorySelection> weightedSelection = new WeightedSelection<DirectorCardCategorySelection>();
		Category[] array = poolCategories;
		foreach (Category category in array)
		{
			float num = SumAllWeightsInCategory(category);
			if (!(num > 0f))
			{
				continue;
			}
			float num2 = category.categoryWeight / num;
			PoolEntry[] alwaysIncluded = category.alwaysIncluded;
			foreach (PoolEntry poolEntry in alwaysIncluded)
			{
				if (poolEntry.dccs.IsAvailable())
				{
					weightedSelection.AddChoice(poolEntry.dccs, poolEntry.weight * num2);
				}
			}
			bool flag = false;
			ConditionalPoolEntry[] includedIfConditionsMet = category.includedIfConditionsMet;
			foreach (ConditionalPoolEntry conditionalPoolEntry in includedIfConditionsMet)
			{
				if (conditionalPoolEntry.dccs.IsAvailable() && AreConditionsMet(conditionalPoolEntry))
				{
					weightedSelection.AddChoice(conditionalPoolEntry.dccs, conditionalPoolEntry.weight * num2);
					flag = true;
				}
			}
			if (flag)
			{
				continue;
			}
			alwaysIncluded = category.includedIfNoConditionsMet;
			foreach (PoolEntry poolEntry2 in alwaysIncluded)
			{
				if (poolEntry2.dccs.IsAvailable())
				{
					weightedSelection.AddChoice(poolEntry2.dccs, poolEntry2.weight * num2);
				}
			}
		}
		return weightedSelection;
	}

	private float SumAllWeightsInCategory(Category category)
	{
		float num = 0f;
		PoolEntry[] alwaysIncluded = category.alwaysIncluded;
		foreach (PoolEntry poolEntry in alwaysIncluded)
		{
			if (poolEntry.dccs.IsAvailable())
			{
				num += poolEntry.weight;
			}
		}
		bool flag = false;
		ConditionalPoolEntry[] includedIfConditionsMet = category.includedIfConditionsMet;
		foreach (ConditionalPoolEntry conditionalPoolEntry in includedIfConditionsMet)
		{
			if (conditionalPoolEntry.dccs.IsAvailable() && AreConditionsMet(conditionalPoolEntry))
			{
				num += conditionalPoolEntry.weight;
				flag = true;
			}
		}
		if (!flag)
		{
			alwaysIncluded = category.includedIfNoConditionsMet;
			foreach (PoolEntry poolEntry2 in alwaysIncluded)
			{
				if (poolEntry2.dccs.IsAvailable())
				{
					num += poolEntry2.weight;
				}
			}
		}
		return num;
	}

	private bool AreConditionsMet(ConditionalPoolEntry entry)
	{
		if (Run.instance != null)
		{
			ExpansionDef[] requiredExpansions = entry.requiredExpansions;
			foreach (ExpansionDef expansionDef in requiredExpansions)
			{
				if (!Run.instance.IsExpansionEnabled(expansionDef))
				{
					return false;
				}
			}
			return true;
		}
		return false;
	}
}

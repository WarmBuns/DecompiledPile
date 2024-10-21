using System;
using HG;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/DCCS/DirectorCardCategorySelection")]
public class DirectorCardCategorySelection : ScriptableObject
{
	public delegate void CalcCardWeight(DirectorCard card, ref float weight);

	[Serializable]
	public struct Category
	{
		[Tooltip("A name to help identify this category")]
		public string name;

		public DirectorCard[] cards;

		public float selectionWeight;
	}

	public Category[] categories = Array.Empty<Category>();

	public static event CalcCardWeight calcCardWeight;

	public float SumAllWeightsInCategory(Category category)
	{
		float num = 0f;
		for (int i = 0; i < category.cards.Length; i++)
		{
			if (category.cards[i].IsAvailable())
			{
				num += (float)category.cards[i].selectionWeight;
			}
		}
		return num;
	}

	public int FindCategoryIndexByName(string categoryName)
	{
		for (int i = 0; i < categories.Length; i++)
		{
			if (string.CompareOrdinal(categories[i].name, categoryName) == 0)
			{
				return i;
			}
		}
		return -1;
	}

	public void CopyFrom([NotNull] DirectorCardCategorySelection src)
	{
		Category[] array = src.categories;
		Array.Resize(ref categories, src.categories.Length);
		for (int i = 0; i < categories.Length; i++)
		{
			ref Category reference = ref categories[i];
			reference = array[i];
			reference.cards = ArrayUtils.Clone(reference.cards);
		}
	}

	public WeightedSelection<DirectorCard> GenerateDirectorCardWeightedSelection()
	{
		WeightedSelection<DirectorCard> weightedSelection = new WeightedSelection<DirectorCard>();
		for (int i = 0; i < categories.Length; i++)
		{
			ref Category reference = ref categories[i];
			float num = SumAllWeightsInCategory(reference);
			float num2 = reference.selectionWeight / num;
			if (!(num > 0f))
			{
				continue;
			}
			DirectorCard[] cards = reference.cards;
			foreach (DirectorCard directorCard in cards)
			{
				if (directorCard.IsAvailable())
				{
					float weight = (float)directorCard.selectionWeight * num2;
					DirectorCardCategorySelection.calcCardWeight?.Invoke(directorCard, ref weight);
					weightedSelection.AddChoice(directorCard, weight);
				}
			}
		}
		return weightedSelection;
	}

	public void Clear()
	{
		categories = Array.Empty<Category>();
	}

	public int AddCategory(string name, float selectionWeight)
	{
		Category category = default(Category);
		category.name = name;
		category.cards = Array.Empty<DirectorCard>();
		category.selectionWeight = selectionWeight;
		Category value = category;
		ArrayUtils.ArrayAppend(ref categories, in value);
		return categories.Length - 1;
	}

	public int AddCard(int categoryIndex, DirectorCard card)
	{
		if ((uint)categoryIndex >= categories.Length)
		{
			throw new ArgumentOutOfRangeException("categoryIndex");
		}
		ref DirectorCard[] cards = ref categories[categoryIndex].cards;
		ArrayUtils.ArrayAppend(ref cards, in card);
		return cards.Length - 1;
	}

	public void RemoveCardsThatFailFilter(Predicate<DirectorCard> predicate)
	{
		for (int i = 0; i < categories.Length; i++)
		{
			ref Category reference = ref categories[i];
			for (int num = reference.cards.Length - 1; num >= 0; num--)
			{
				DirectorCard obj = reference.cards[num];
				if (!predicate(obj))
				{
					ArrayUtils.ArrayRemoveAtAndResize(ref reference.cards, num);
				}
			}
		}
	}

	public virtual bool IsAvailable()
	{
		return true;
	}

	public virtual void OnSelected(ClassicStageInfo stageInfo)
	{
	}

	public void OnValidate()
	{
		for (int i = 0; i < categories.Length; i++)
		{
			Category category = categories[i];
			if (category.selectionWeight <= 0f)
			{
				Debug.LogErrorFormat("'{0}' in '{1}' has no weight!", category.name, this);
			}
			for (int j = 0; j < category.cards.Length; j++)
			{
				DirectorCard directorCard = category.cards[j];
				if ((float)directorCard.selectionWeight <= 0f)
				{
					Debug.LogErrorFormat("'{0}' in '{1}' has no weight!", directorCard.spawnCard.name, this);
				}
			}
		}
	}
}

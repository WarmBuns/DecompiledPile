using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/DropTables/BasicPickupDropTable")]
public class BasicPickupDropTable : PickupDropTable
{
	public ItemTag[] requiredItemTags = Array.Empty<ItemTag>();

	public ItemTag[] bannedItemTags = Array.Empty<ItemTag>();

	public float tier1Weight = 0.8f;

	public float tier2Weight = 0.2f;

	public float tier3Weight = 0.01f;

	public float bossWeight;

	public float lunarEquipmentWeight;

	public float lunarItemWeight;

	public float lunarCombinedWeight;

	public float equipmentWeight;

	public float voidTier1Weight;

	public float voidTier2Weight;

	public float voidTier3Weight;

	public float voidBossWeight;

	private readonly WeightedSelection<PickupIndex> selector = new WeightedSelection<PickupIndex>();

	protected override void Regenerate(Run run)
	{
		GenerateWeightedSelection(run);
	}

	public void RegenerateDropTable(Run run)
	{
		GenerateWeightedSelection(run);
	}

	public bool IsFilterRequired()
	{
		if (requiredItemTags.Length == 0)
		{
			return bannedItemTags.Length != 0;
		}
		return true;
	}

	public bool PassesFilter(PickupIndex pickupIndex)
	{
		PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
		if (pickupDef.itemIndex != ItemIndex.None)
		{
			ItemDef itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);
			ItemTag[] array = requiredItemTags;
			foreach (ItemTag value in array)
			{
				if (Array.IndexOf(itemDef.tags, value) == -1)
				{
					return false;
				}
			}
			array = bannedItemTags;
			foreach (ItemTag value2 in array)
			{
				if (Array.IndexOf(itemDef.tags, value2) != -1)
				{
					return false;
				}
			}
		}
		return true;
	}

	private void Add(List<PickupIndex> sourceDropList, float chance)
	{
		if (chance <= 0f || sourceDropList.Count == 0)
		{
			return;
		}
		foreach (PickupIndex sourceDrop in sourceDropList)
		{
			if (!IsFilterRequired() || PassesFilter(sourceDrop))
			{
				selector.AddChoice(sourceDrop, chance);
			}
		}
	}

	private void GenerateWeightedSelection(Run run)
	{
		selector.Clear();
		Add(run.availableTier1DropList, tier1Weight);
		Add(run.availableTier2DropList, tier2Weight);
		Add(run.availableTier3DropList, tier3Weight);
		Add(run.availableBossDropList, bossWeight);
		Add(run.availableLunarItemDropList, lunarItemWeight);
		Add(run.availableLunarEquipmentDropList, lunarEquipmentWeight);
		Add(run.availableLunarCombinedDropList, lunarCombinedWeight);
		Add(run.availableEquipmentDropList, equipmentWeight);
		Add(run.availableVoidTier1DropList, voidTier1Weight);
		Add(run.availableVoidTier2DropList, voidTier2Weight);
		Add(run.availableVoidTier3DropList, voidTier3Weight);
		Add(run.availableVoidBossDropList, voidBossWeight);
	}

	protected override PickupIndex GenerateDropPreReplacement(Xoroshiro128Plus rng)
	{
		return PickupDropTable.GenerateDropFromWeightedSelection(rng, selector);
	}

	public override int GetPickupCount()
	{
		return selector.Count;
	}

	protected override PickupIndex[] GenerateUniqueDropsPreReplacement(int maxDrops, Xoroshiro128Plus rng)
	{
		return PickupDropTable.GenerateUniqueDropsFromWeightedSelection(maxDrops, rng, selector);
	}
}

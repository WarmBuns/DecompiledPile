using System.Collections.Generic;

namespace RoR2.Items;

public static class RandomlyLunarUtils
{
	private const float replacePercentagePerStack = 0.05f;

	public static PickupIndex CheckForLunarReplacement(PickupIndex pickupIndex, Xoroshiro128Plus rng)
	{
		PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
		if (CanReplace(pickupDef))
		{
			int itemCountGlobal = Util.GetItemCountGlobal(DLC1Content.Items.RandomlyLunar.itemIndex, requiresAlive: false, requiresConnected: false);
			if (itemCountGlobal > 0)
			{
				List<PickupIndex> list = null;
				if (pickupDef.itemIndex != ItemIndex.None)
				{
					list = Run.instance.availableLunarItemDropList;
				}
				else if (pickupDef.equipmentIndex != EquipmentIndex.None)
				{
					list = Run.instance.availableLunarEquipmentDropList;
				}
				if (list != null && list.Count > 0 && rng.nextNormalizedFloat < 0.05f * (float)itemCountGlobal)
				{
					int index = rng.RangeInt(0, list.Count);
					return list[index];
				}
			}
		}
		return pickupIndex;
	}

	public static void CheckForLunarReplacementUniqueArray(PickupIndex[] pickupIndices, Xoroshiro128Plus rng)
	{
		int itemCountGlobal = Util.GetItemCountGlobal(DLC1Content.Items.RandomlyLunar.itemIndex, requiresAlive: false, requiresConnected: false);
		if (itemCountGlobal <= 0)
		{
			return;
		}
		List<PickupIndex> list = null;
		List<PickupIndex> list2 = null;
		for (int i = 0; i < pickupIndices.Length; i++)
		{
			PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndices[i]);
			if (!CanReplace(pickupDef) || !(rng.nextNormalizedFloat < 0.05f * (float)itemCountGlobal))
			{
				continue;
			}
			List<PickupIndex> list3 = null;
			if (pickupDef.itemIndex != ItemIndex.None)
			{
				if (list == null)
				{
					list = new List<PickupIndex>(Run.instance.availableLunarItemDropList);
					Util.ShuffleList(list, rng);
				}
				list3 = list;
			}
			else if (pickupDef.equipmentIndex != EquipmentIndex.None)
			{
				if (list2 == null)
				{
					list2 = new List<PickupIndex>(Run.instance.availableLunarEquipmentDropList);
					Util.ShuffleList(list2, rng);
				}
				list3 = list2;
			}
			if (list3 != null && list3.Count > 0)
			{
				pickupIndices[i] = list3[i % list3.Count];
			}
		}
	}

	public static bool CanReplace(PickupDef pickupDef)
	{
		return !(pickupDef?.isLunar ?? true);
	}
}

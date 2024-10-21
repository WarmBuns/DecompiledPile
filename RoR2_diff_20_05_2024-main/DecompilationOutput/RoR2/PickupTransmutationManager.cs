using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HG;
using JetBrains.Annotations;

namespace RoR2;

public static class PickupTransmutationManager
{
	private static PickupIndex[][] pickupGroups = Array.Empty<PickupIndex[]>();

	private static PickupIndex[][] pickupGroupMap = Array.Empty<PickupIndex[]>();

	private static PickupIndex[] itemTier1Group;

	private static PickupIndex[] itemTier2Group;

	private static PickupIndex[] itemTier3Group;

	private static PickupIndex[] itemTierBossGroup;

	private static PickupIndex[] itemTierLunarGroup;

	private static PickupIndex[] equipmentNormalGroup;

	private static PickupIndex[] equipmentLunarGroup;

	private static PickupIndex[] equipmentBossGroup;

	private static PickupIndex[] itemVoidTier1Group;

	private static PickupIndex[] itemVoidTier2Group;

	private static PickupIndex[] itemVoidTier3Group;

	private static PickupIndex[] itemVoidBossGroup;

	private static PickupIndex[][] availablePickupGroups = Array.Empty<PickupIndex[]>();

	private static PickupIndex[][] availablePickupGroupMap = Array.Empty<PickupIndex[]>();

	[SystemInitializer(new Type[] { typeof(PickupCatalog) })]
	public static IEnumerator Init()
	{
		RebuildPickupGroups();
		Run.onRunStartGlobal += RebuildAvailablePickupGroups;
		yield return null;
	}

	public static void RebuildPickupGroups()
	{
		pickupGroupMap = new PickupIndex[PickupCatalog.pickupCount][];
		List<PickupIndex[]> groups = new List<PickupIndex[]>();
		itemTier1Group = AddItemTierGroup(ItemTier.Tier1);
		itemTier2Group = AddItemTierGroup(ItemTier.Tier2);
		itemTier3Group = AddItemTierGroup(ItemTier.Tier3);
		itemTierBossGroup = AddItemTierGroup(ItemTier.Boss);
		itemTierLunarGroup = AddItemTierGroup(ItemTier.Lunar);
		equipmentNormalGroup = AddGroup(GetEquipmentGroup(isBoss: false, isLunar: false));
		equipmentLunarGroup = AddGroup(GetEquipmentGroup(isBoss: false, isLunar: true));
		equipmentBossGroup = AddGroup(GetEquipmentGroup(isBoss: true, isLunar: false));
		itemVoidTier1Group = AddItemTierGroup(ItemTier.VoidTier1);
		itemVoidTier2Group = AddItemTierGroup(ItemTier.VoidTier2);
		itemVoidTier3Group = AddItemTierGroup(ItemTier.VoidTier3);
		itemVoidBossGroup = AddItemTierGroup(ItemTier.VoidBoss);
		pickupGroups = groups.ToArray();
		PickupIndex[] AddGroup(PickupIndex[] group)
		{
			groups.Add(group);
			for (int i = 0; i < group.Length; i++)
			{
				PickupIndex pickupIndex2 = group[i];
				pickupGroupMap[pickupIndex2.value] = group;
			}
			return group;
		}
		PickupIndex[] AddItemTierGroup(ItemTier tier)
		{
			PickupIndex[] array = GetItemTierGroup(tier);
			AddGroup(array);
			PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(tier);
			if (pickupIndex != PickupIndex.none)
			{
				pickupGroupMap[pickupIndex.value] = array;
			}
			return array;
		}
		static PickupIndex[] GetEquipmentGroup(bool isBoss, bool isLunar)
		{
			return (from equipmentDef in EquipmentCatalog.allEquipment.Select(EquipmentCatalog.GetEquipmentDef)
				where equipmentDef.canDrop && equipmentDef.isBoss == isBoss && equipmentDef.isLunar == isLunar
				select PickupCatalog.FindPickupIndex(equipmentDef.equipmentIndex)).ToArray();
		}
		static PickupIndex[] GetItemTierGroup(ItemTier itemTier)
		{
			return (from itemDef in ItemCatalog.allItems.Select(ItemCatalog.GetItemDef)
				where itemDef.tier == itemTier && !itemDef.ContainsTag(ItemTag.WorldUnique)
				select PickupCatalog.FindPickupIndex(itemDef.itemIndex)).ToArray();
		}
	}

	private static void RebuildAvailablePickupGroups([NotNull] Run source)
	{
		Array.Resize(ref availablePickupGroups, pickupGroups.Length);
		Array.Resize(ref availablePickupGroupMap, pickupGroupMap.Length);
		Func<PickupIndex, bool> predicate = source.IsPickupAvailable;
		for (int i = 0; i < availablePickupGroups.Length; i++)
		{
			PickupIndex[] array = pickupGroups[i];
			PickupIndex[] array2 = array.Where(predicate).ToArray();
			availablePickupGroups[i] = array2;
			for (int j = 0; j < array.Length; j++)
			{
				availablePickupGroupMap[array[j].value] = array2;
			}
		}
	}

	[CanBeNull]
	public static PickupIndex[] GetGroupFromPickupIndex(PickupIndex pickupIndex)
	{
		return ArrayUtils.GetSafe(pickupGroupMap, pickupIndex.value);
	}

	[CanBeNull]
	public static PickupIndex[] GetAvailableGroupFromPickupIndex(PickupIndex pickupIndex)
	{
		return ArrayUtils.GetSafe(availablePickupGroupMap, pickupIndex.value);
	}
}

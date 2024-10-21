using System;
using System.Collections.Generic;
using HG;
using RoR2.ContentManagement;
using UnityEngine;

namespace RoR2;

public static class ItemTierCatalog
{
	private static ItemTierDef[] itemTierDefs = Array.Empty<ItemTierDef>();

	public static ResourceAvailability availability = default(ResourceAvailability);

	private static readonly Dictionary<ItemTier, ItemTierDef> itemTierToDef = new Dictionary<ItemTier, ItemTierDef>();

	public static ReadOnlyArray<ItemTierDef> allItemTierDefs => itemTierDefs;

	public static int itemCount => itemTierDefs.Length;

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		ArrayUtils.CloneTo(ContentManager.itemTierDefs, ref itemTierDefs);
		itemTierToDef.Clear();
		int num = 0;
		ItemTierDef[] array = itemTierDefs;
		foreach (ItemTierDef itemTierDef in array)
		{
			if (itemTierDef.tier == ItemTier.AssignedAtRuntime)
			{
				itemTierDef.tier = (ItemTier)(++num + 10);
			}
			if (itemTierToDef.ContainsKey(itemTierDef.tier))
			{
				Debug.LogError($"Duplicate TierDef for tier {itemTierDef.tier}");
			}
			else
			{
				itemTierToDef.Add(itemTierDef.tier, itemTierDef);
			}
		}
		availability.MakeAvailable();
	}

	public static ItemTierDef GetItemTierDef(ItemTier itemTier)
	{
		if (itemTierToDef.TryGetValue(itemTier, out var value))
		{
			return value;
		}
		return null;
	}

	public static ItemTierDef FindTierDef(string tierName)
	{
		ItemTierDef[] array = itemTierDefs;
		foreach (ItemTierDef itemTierDef in array)
		{
			if (itemTierDef.name == tierName)
			{
				return itemTierDef;
			}
		}
		return null;
	}
}

using System;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/DropTables/ExplicitPickupDropTable")]
public class ExplicitPickupDropTable : PickupDropTable
{
	[Serializable]
	public struct StringEntry
	{
		public string pickupName;

		public float pickupWeight;
	}

	[Serializable]
	public struct PickupDefEntry
	{
		[TypeRestrictedReference(new Type[]
		{
			typeof(ItemDef),
			typeof(EquipmentDef),
			typeof(MiscPickupDef)
		})]
		public UnityEngine.Object pickupDef;

		public float pickupWeight;
	}

	public PickupDefEntry[] pickupEntries = Array.Empty<PickupDefEntry>();

	[Header("Deprecated")]
	[Obsolete("Use pickupEntries instead.", false)]
	public StringEntry[] entries = Array.Empty<StringEntry>();

	private readonly WeightedSelection<PickupIndex> weightedSelection = new WeightedSelection<PickupIndex>();

	protected override void Regenerate(Run run)
	{
		GenerateWeightedSelection();
	}

	private void GenerateWeightedSelection()
	{
		weightedSelection.Clear();
		StringEntry[] array = entries;
		for (int i = 0; i < array.Length; i++)
		{
			StringEntry stringEntry = array[i];
			weightedSelection.AddChoice(PickupCatalog.FindPickupIndex(stringEntry.pickupName), stringEntry.pickupWeight);
		}
		PickupDefEntry[] array2 = pickupEntries;
		for (int i = 0; i < array2.Length; i++)
		{
			PickupDefEntry pickupDefEntry = array2[i];
			PickupIndex pickupIndex = PickupIndex.none;
			UnityEngine.Object pickupDef = pickupDefEntry.pickupDef;
			if (!(pickupDef is ItemDef itemDef))
			{
				if (pickupDef is EquipmentDef equipmentDef)
				{
					pickupIndex = PickupCatalog.FindPickupIndex(equipmentDef.equipmentIndex);
				}
				else
				{
					MiscPickupDef miscPickupDef = pickupDefEntry.pickupDef as MiscPickupDef;
					if (miscPickupDef != null)
					{
						pickupIndex = PickupCatalog.FindPickupIndex(miscPickupDef.miscPickupIndex);
					}
				}
			}
			else
			{
				pickupIndex = PickupCatalog.FindPickupIndex(itemDef.itemIndex);
			}
			if (pickupIndex != PickupIndex.none)
			{
				weightedSelection.AddChoice(pickupIndex, pickupDefEntry.pickupWeight);
			}
		}
	}

	protected override PickupIndex GenerateDropPreReplacement(Xoroshiro128Plus rng)
	{
		return PickupDropTable.GenerateDropFromWeightedSelection(rng, weightedSelection);
	}

	public override int GetPickupCount()
	{
		return weightedSelection.Count;
	}

	protected override PickupIndex[] GenerateUniqueDropsPreReplacement(int maxDrops, Xoroshiro128Plus rng)
	{
		return PickupDropTable.GenerateUniqueDropsFromWeightedSelection(maxDrops, rng, weightedSelection);
	}
}

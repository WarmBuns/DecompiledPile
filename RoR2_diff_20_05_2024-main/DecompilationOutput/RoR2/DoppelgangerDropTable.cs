using System;
using RoR2.Artifacts;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/DropTables/DoppelgangerDropTable")]
public class DoppelgangerDropTable : PickupDropTable
{
	public ItemTag[] requiredItemTags = Array.Empty<ItemTag>();

	public ItemTag[] bannedItemTags = Array.Empty<ItemTag>();

	public float tier1Weight = 0.8f;

	public float tier2Weight = 0.2f;

	public float tier3Weight = 0.01f;

	public float bossWeight;

	public float lunarItemWeight;

	public float voidTier1Weight;

	public float voidTier2Weight;

	public float voidTier3Weight;

	public float voidBossWeight;

	private readonly WeightedSelection<PickupIndex> selector = new WeightedSelection<PickupIndex>();

	private Inventory doppelgangerInventory;

	protected override void OnEnable()
	{
		base.OnEnable();
		DoppelgangerInvasionManager.onDoppelgangerDeath += OnDoppelgangerDeath;
	}

	protected override void OnDisable()
	{
		DoppelgangerInvasionManager.onDoppelgangerDeath -= OnDoppelgangerDeath;
		base.OnDisable();
	}

	protected override void Regenerate(Run run)
	{
		GenerateWeightedSelection();
	}

	private bool CanSelectItem(ItemDef itemDef)
	{
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
		return itemDef.canRemove;
	}

	private void GenerateWeightedSelection()
	{
		selector.Clear();
		if (!doppelgangerInventory)
		{
			return;
		}
		foreach (ItemIndex item in doppelgangerInventory.itemAcquisitionOrder)
		{
			ItemDef itemDef = ItemCatalog.GetItemDef(item);
			PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(item);
			if (!CanSelectItem(itemDef))
			{
				continue;
			}
			float num = 0f;
			switch (itemDef.tier)
			{
			case ItemTier.Tier1:
				if (Run.instance.availableTier1DropList.Contains(pickupIndex))
				{
					num = tier1Weight;
				}
				break;
			case ItemTier.Tier2:
				if (Run.instance.availableTier2DropList.Contains(pickupIndex))
				{
					num = tier2Weight;
				}
				break;
			case ItemTier.Tier3:
				if (Run.instance.availableTier3DropList.Contains(pickupIndex))
				{
					num = tier3Weight;
				}
				break;
			case ItemTier.Boss:
				if (Run.instance.availableBossDropList.Contains(pickupIndex))
				{
					num = bossWeight;
				}
				break;
			case ItemTier.Lunar:
				if (Run.instance.availableLunarItemDropList.Contains(pickupIndex))
				{
					num = lunarItemWeight;
				}
				break;
			case ItemTier.VoidTier1:
				if (Run.instance.availableVoidTier1DropList.Contains(pickupIndex))
				{
					num = voidTier1Weight;
				}
				break;
			case ItemTier.VoidTier2:
				if (Run.instance.availableVoidTier2DropList.Contains(pickupIndex))
				{
					num = voidTier2Weight;
				}
				break;
			case ItemTier.VoidTier3:
				if (Run.instance.availableVoidTier3DropList.Contains(pickupIndex))
				{
					num = voidTier3Weight;
				}
				break;
			case ItemTier.VoidBoss:
				if (Run.instance.availableVoidTier3DropList.Contains(pickupIndex))
				{
					num = voidBossWeight;
				}
				break;
			}
			if (num > 0f)
			{
				selector.AddChoice(pickupIndex, num);
			}
		}
	}

	protected override PickupIndex GenerateDropPreReplacement(Xoroshiro128Plus rng)
	{
		return PickupDropTable.GenerateDropFromWeightedSelection(rng, selector);
	}

	private void OnDoppelgangerDeath(DamageReport damageReport)
	{
		doppelgangerInventory = damageReport.victimMaster?.inventory;
		GenerateWeightedSelection();
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

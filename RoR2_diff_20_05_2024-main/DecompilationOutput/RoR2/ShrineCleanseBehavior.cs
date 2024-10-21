using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class ShrineCleanseBehavior : MonoBehaviour, IInteractable
{
	public string contextToken;

	public GameObject activationEffectPrefab;

	private static ItemIndex[] cleansableItems = Array.Empty<ItemIndex>();

	private static EquipmentIndex[] cleansableEquipments = Array.Empty<EquipmentIndex>();

	public string GetContextString(Interactor activator)
	{
		return Language.GetString(contextToken);
	}

	private static bool InventoryIsCleansable(Inventory inventory)
	{
		for (int i = 0; i < cleansableItems.Length; i++)
		{
			if (inventory.GetItemCount(cleansableItems[i]) > 0)
			{
				return true;
			}
		}
		int j = 0;
		for (int equipmentSlotCount = inventory.GetEquipmentSlotCount(); j < equipmentSlotCount; j++)
		{
			EquipmentState equipment = inventory.GetEquipment((uint)j);
			for (int k = 0; k < cleansableEquipments.Length; k++)
			{
				if (equipment.equipmentIndex == cleansableEquipments[k])
				{
					return true;
				}
			}
		}
		return false;
	}

	private static int CleanseInventoryServer(Inventory inventory)
	{
		int num = 0;
		for (int i = 0; i < cleansableItems.Length; i++)
		{
			ItemIndex itemIndex = cleansableItems[i];
			int itemCount = inventory.GetItemCount(itemIndex);
			if (itemCount != 0)
			{
				inventory.RemoveItem(itemIndex, itemCount);
				num += itemCount;
			}
		}
		int num2 = 0;
		int j = 0;
		for (int equipmentSlotCount = inventory.GetEquipmentSlotCount(); j < equipmentSlotCount; j++)
		{
			EquipmentState equipment = inventory.GetEquipment((uint)j);
			for (int k = 0; k < cleansableEquipments.Length; k++)
			{
				if (equipment.equipmentIndex == cleansableEquipments[k])
				{
					inventory.SetEquipment(EquipmentState.empty, (uint)j);
					num2++;
				}
			}
		}
		return num + num2;
	}

	public Interactability GetInteractability(Interactor activator)
	{
		CharacterBody component = activator.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			Inventory inventory = component.inventory;
			if ((bool)inventory && InventoryIsCleansable(inventory))
			{
				return Interactability.Available;
			}
		}
		return Interactability.ConditionsNotMet;
	}

	public void OnInteractionBegin(Interactor activator)
	{
		CharacterBody component = activator.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			Inventory inventory = component.inventory;
			if ((bool)inventory)
			{
				CleanseInventoryServer(inventory);
				EffectManager.SimpleEffect(activationEffectPrefab, base.transform.position, base.transform.rotation, transmit: true);
			}
		}
	}

	public bool ShouldIgnoreSpherecastForInteractibility(Interactor activator)
	{
		return false;
	}

	public bool ShouldShowOnScanner()
	{
		return true;
	}

	public bool ShouldProximityHighlight()
	{
		return true;
	}

	[SystemInitializer(new Type[]
	{
		typeof(ItemCatalog),
		typeof(EquipmentCatalog)
	})]
	private static void Init()
	{
		List<ItemIndex> list = new List<ItemIndex>();
		List<EquipmentIndex> list2 = new List<EquipmentIndex>();
		ItemIndex itemIndex = ItemIndex.Count;
		for (ItemIndex itemCount = (ItemIndex)ItemCatalog.itemCount; itemIndex < itemCount; itemIndex++)
		{
			ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
			if (itemDef.tier == ItemTier.Lunar || itemDef.ContainsTag(ItemTag.Cleansable))
			{
				list.Add(itemIndex);
			}
		}
		EquipmentIndex equipmentIndex = (EquipmentIndex)0;
		for (EquipmentIndex equipmentCount = (EquipmentIndex)EquipmentCatalog.equipmentCount; equipmentIndex < equipmentCount; equipmentIndex++)
		{
			if (EquipmentCatalog.GetEquipmentDef(equipmentIndex).isLunar)
			{
				list2.Add(equipmentIndex);
			}
		}
		cleansableItems = list.ToArray();
		cleansableEquipments = list2.ToArray();
	}
}

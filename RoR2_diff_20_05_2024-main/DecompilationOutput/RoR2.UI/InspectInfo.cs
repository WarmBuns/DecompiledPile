using System;
using UnityEngine;

namespace RoR2.UI;

[Serializable]
public class InspectInfo
{
	public Sprite Visual;

	public string TitleToken;

	public string DescriptionToken;

	public string FlavorToken;

	public Color TitleColor = Color.white;

	public bool isConsumedItem;

	[NonSerialized]
	public bool isDynamicInspectInfo;

	[NonSerialized]
	public PickupIndex dynamicInspectPickupIndex;

	public bool ForceShowInfo { get; private set; }

	private InspectInfo()
	{
	}

	public static implicit operator InspectInfo(PickupDef fromPickup)
	{
		if (fromPickup != null)
		{
			ItemDef itemDef = ItemCatalog.GetItemDef(fromPickup.itemIndex);
			if (itemDef != null)
			{
				return itemDef;
			}
			EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(fromPickup.equipmentIndex);
			if (equipmentDef != null)
			{
				return equipmentDef;
			}
			MiscPickupDef miscDef = MiscPickupCatalog.GetMiscDef(fromPickup.miscPickupIndex);
			if (miscDef != null)
			{
				return miscDef;
			}
		}
		return new InspectInfo();
	}

	public static implicit operator InspectInfo(ItemDef fromItem)
	{
		return new InspectInfo
		{
			Visual = fromItem.pickupIconSprite,
			TitleToken = fromItem.nameToken,
			DescriptionToken = fromItem.descriptionToken,
			FlavorToken = fromItem.loreToken,
			TitleColor = ColorCatalog.GetColor(ItemTierCatalog.GetItemTierDef(fromItem.tier)?.colorIndex ?? ColorCatalog.ColorIndex.Tier1Item),
			isDynamicInspectInfo = true,
			dynamicInspectPickupIndex = PickupCatalog.FindPickupIndex(fromItem.itemIndex),
			isConsumedItem = fromItem.isConsumed
		};
	}

	public static implicit operator InspectInfo(EquipmentDef fromEquip)
	{
		return new InspectInfo
		{
			Visual = fromEquip.pickupIconSprite,
			TitleToken = fromEquip.nameToken,
			DescriptionToken = fromEquip.descriptionToken,
			FlavorToken = fromEquip.loreToken,
			TitleColor = ColorCatalog.GetColor(fromEquip.colorIndex),
			isDynamicInspectInfo = true,
			dynamicInspectPickupIndex = PickupCatalog.FindPickupIndex(fromEquip.equipmentIndex),
			isConsumedItem = fromEquip.isConsumed
		};
	}

	public static implicit operator InspectInfo(MiscPickupDef fromMisc)
	{
		return new InspectInfo
		{
			Visual = fromMisc.visual,
			TitleToken = fromMisc.nameToken,
			DescriptionToken = fromMisc.descriptionToken,
			TitleColor = ColorCatalog.GetColor(fromMisc.darkColor)
		};
	}

	public static implicit operator InspectInfo(InspectDef fromDef)
	{
		if (fromDef != null)
		{
			return fromDef.Info;
		}
		return null;
	}

	public void MarkForceShowInfo()
	{
		ForceShowInfo = true;
	}
}

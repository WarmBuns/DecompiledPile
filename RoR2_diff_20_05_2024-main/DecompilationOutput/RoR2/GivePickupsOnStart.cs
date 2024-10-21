using System;
using HG;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(Inventory))]
public class GivePickupsOnStart : MonoBehaviour
{
	[Serializable]
	public struct ItemInfo
	{
		public string itemString;

		public int count;
	}

	[Serializable]
	public struct ItemDefInfo
	{
		public ItemDef itemDef;

		public int count;

		public bool dontExceedCount;
	}

	public EquipmentDef equipmentDef;

	[ShowFieldObsolete]
	public string equipmentString;

	public bool overwriteEquipment = true;

	public ItemDefInfo[] itemDefInfos;

	[ShowFieldObsolete]
	public ItemInfo[] itemInfos;

	private Inventory inventory;

	private void Start()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		inventory = GetComponent<Inventory>();
		if (overwriteEquipment || inventory.currentEquipmentIndex == EquipmentIndex.None)
		{
			if ((bool)equipmentDef)
			{
				inventory.SetEquipmentIndex(equipmentDef.equipmentIndex);
			}
			else if (!string.IsNullOrEmpty(equipmentString))
			{
				inventory.GiveEquipmentString(equipmentString);
			}
		}
		ItemDefInfo[] array = itemDefInfos;
		for (int i = 0; i < array.Length; i++)
		{
			ItemDefInfo itemDefInfo = array[i];
			if ((bool)itemDefInfo.itemDef)
			{
				int num = itemDefInfo.count;
				if (itemDefInfo.dontExceedCount)
				{
					num = Math.Max(itemDefInfo.count - inventory.GetItemCount(itemDefInfo.itemDef), 0);
				}
				if (num != 0)
				{
					inventory.GiveItem(itemDefInfo.itemDef, num);
				}
			}
		}
		for (int j = 0; j < itemInfos.Length; j++)
		{
			ItemInfo itemInfo = itemInfos[j];
			inventory.GiveItemString(itemInfo.itemString, itemInfo.count);
		}
	}
}

using System;
using System.Collections.Generic;
using HG;
using UnityEngine.Networking;

namespace RoR2.Items;

public static class ContagiousItemManager
{
	public struct TransformationInfo
	{
		public ItemIndex originalItem;

		public ItemIndex transformedItem;
	}

	private struct InventoryReplacementCandidate
	{
		public Inventory inventory;

		public ItemIndex originalItem;

		public Run.FixedTimeStamp time;

		public bool isForced;
	}

	public static readonly float transformDelay = 0.5f;

	private static ItemIndex[] originalToTransformed = Array.Empty<ItemIndex>();

	private static bool[] itemsToCheck = Array.Empty<bool>();

	private static TransformationInfo[] _transformationInfos = Array.Empty<TransformationInfo>();

	public static ReadOnlyArray<TransformationInfo> transformationInfos = new ReadOnlyArray<TransformationInfo>(_transformationInfos);

	private static List<InventoryReplacementCandidate> pendingChanges = new List<InventoryReplacementCandidate>();

	[SystemInitializer(new Type[] { typeof(ItemCatalog) })]
	private static void Init()
	{
		InitTransformationTable();
		Inventory.onInventoryChangedGlobal += OnInventoryChangedGlobal;
		RoR2Application.onFixedUpdate += StaticFixedUpdate;
	}

	private static void StaticFixedUpdate()
	{
		if (pendingChanges.Count > 0)
		{
			ProcessPendingChanges();
		}
	}

	private static void InitTransformationTable()
	{
		originalToTransformed = new ItemIndex[ItemCatalog.itemCount];
		ArrayUtils.SetAll(originalToTransformed, ItemIndex.None);
		itemsToCheck = new bool[ItemCatalog.itemCount];
		foreach (ItemDef.Pair item in ItemCatalog.GetItemPairsForRelationship(DLC1Content.ItemRelationshipTypes.ContagiousItem))
		{
			originalToTransformed[(int)item.itemDef1.itemIndex] = item.itemDef2.itemIndex;
			itemsToCheck[(int)item.itemDef1.itemIndex] = true;
			itemsToCheck[(int)item.itemDef2.itemIndex] = true;
			ArrayUtils.ArrayAppend(ref _transformationInfos, new TransformationInfo
			{
				originalItem = item.itemDef1.itemIndex,
				transformedItem = item.itemDef2.itemIndex
			});
		}
		transformationInfos = new ReadOnlyArray<TransformationInfo>(_transformationInfos);
	}

	public static ItemIndex GetTransformedItemIndex(ItemIndex itemIndex)
	{
		if (itemIndex >= ItemIndex.Count && (int)itemIndex < originalToTransformed.Length)
		{
			return originalToTransformed[(int)itemIndex];
		}
		return ItemIndex.None;
	}

	public static ItemIndex GetOriginalItemIndex(ItemIndex transformedItemIndex)
	{
		TransformationInfo[] array = _transformationInfos;
		for (int i = 0; i < array.Length; i++)
		{
			TransformationInfo transformationInfo = array[i];
			if (transformedItemIndex == transformationInfo.transformedItem)
			{
				return transformationInfo.originalItem;
			}
		}
		return ItemIndex.None;
	}

	private static int FindInventoryReplacementCandidateIndex(Inventory inventory, ItemIndex originalItem)
	{
		for (int i = 0; i < pendingChanges.Count; i++)
		{
			InventoryReplacementCandidate inventoryReplacementCandidate = pendingChanges[i];
			if ((object)inventoryReplacementCandidate.inventory == inventory && inventoryReplacementCandidate.originalItem == originalItem)
			{
				return i;
			}
		}
		return -1;
	}

	private static void ProcessPendingChanges()
	{
		if (!NetworkServer.active || !Run.instance)
		{
			pendingChanges.Clear();
			return;
		}
		for (int num = pendingChanges.Count - 1; num >= 0; num--)
		{
			InventoryReplacementCandidate value = pendingChanges[num];
			if (value.time.hasPassed)
			{
				if (!StepInventoryInfection(value.inventory, value.originalItem, int.MaxValue, value.isForced))
				{
					pendingChanges.RemoveAt(num);
				}
				else
				{
					value.time = Run.FixedTimeStamp.now + transformDelay;
					pendingChanges[num] = value;
				}
			}
		}
	}

	private static void OnInventoryChangedGlobal(Inventory inventory)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		for (int i = 0; i < _transformationInfos.Length; i++)
		{
			ref TransformationInfo reference = ref _transformationInfos[i];
			if (inventory.GetItemCount(reference.transformedItem) > 0)
			{
				TryQueueReplacement(inventory, reference.originalItem, reference.transformedItem, isForced: false);
			}
		}
	}

	private static void TryQueueReplacement(Inventory inventory, ItemIndex originalItemIndex, ItemIndex transformedItemIndex, bool isForced)
	{
		if (inventory.GetItemCount(originalItemIndex) > 0 && FindInventoryReplacementCandidateIndex(inventory, transformedItemIndex) == -1)
		{
			pendingChanges.Add(new InventoryReplacementCandidate
			{
				inventory = inventory,
				originalItem = originalItemIndex,
				time = Run.FixedTimeStamp.now + transformDelay,
				isForced = isForced
			});
		}
	}

	public static void TryForceReplacement(Inventory inventory, ItemIndex originalItemIndex)
	{
		ItemIndex transformedItemIndex = GetTransformedItemIndex(originalItemIndex);
		if (transformedItemIndex != ItemIndex.None && Run.instance.IsItemAvailable(transformedItemIndex))
		{
			TryQueueReplacement(inventory, originalItemIndex, transformedItemIndex, isForced: true);
		}
	}

	private static bool StepInventoryInfection(Inventory inventory, ItemIndex originalItem, int limit, bool isForced)
	{
		ItemIndex itemIndex = originalToTransformed[(int)originalItem];
		int itemCount = inventory.GetItemCount(itemIndex);
		if (isForced || itemCount > 0)
		{
			int itemCount2 = inventory.GetItemCount(originalItem);
			int num = Math.Min(limit, itemCount2);
			if (num > 0)
			{
				inventory.RemoveItem(originalItem, num);
				inventory.GiveItem(itemIndex, num);
				CharacterMaster component = inventory.GetComponent<CharacterMaster>();
				if ((bool)component)
				{
					CharacterMasterNotificationQueue.SendTransformNotification(component, originalItem, itemIndex, CharacterMasterNotificationQueue.TransformationType.ContagiousVoid);
				}
				return true;
			}
		}
		return false;
	}
}

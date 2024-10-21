using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using HG;
using RoR2.ContentManagement;
using RoR2.Modding;
using UnityEngine;

namespace RoR2;

public static class ItemCatalog
{
	public struct AllItemsEnumerator : IEnumerator<ItemIndex>, IEnumerator, IDisposable
	{
		private ItemIndex position;

		public ItemIndex Current => position;

		object IEnumerator.Current => Current;

		public bool MoveNext()
		{
			position++;
			return (int)position < itemCount;
		}

		public void Reset()
		{
			position = ItemIndex.None;
		}

		void IDisposable.Dispose()
		{
		}
	}

	public static List<ItemIndex> tier1ItemList = new List<ItemIndex>();

	public static List<ItemIndex> tier2ItemList = new List<ItemIndex>();

	public static List<ItemIndex> tier3ItemList = new List<ItemIndex>();

	public static List<ItemIndex> lunarItemList = new List<ItemIndex>();

	public static ResourceAvailability availability = default(ResourceAvailability);

	public static string[] itemNames = Array.Empty<string>();

	private static readonly Dictionary<string, ItemIndex> itemNameToIndex = new Dictionary<string, ItemIndex>();

	private static ItemIndex[][] itemIndicesByTag = Array.Empty<ItemIndex[]>();

	private static Dictionary<ItemRelationshipType, ItemDef.Pair[]> itemRelationships = new Dictionary<ItemRelationshipType, ItemDef.Pair[]>();

	private static readonly Stack<ItemIndex[]> itemOrderBuffers = new Stack<ItemIndex[]>();

	private static readonly Stack<int[]> itemStackArrays = new Stack<int[]>();

	public static readonly GenericStaticEnumerable<ItemIndex, AllItemsEnumerator> allItems;

	public static int itemCount
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (ContentManager._itemDefs == null)
			{
				return 0;
			}
			return ContentManager._itemDefs.Length;
		}
	}

	public static ReadOnlyArray<ItemDef> allItemDefs => ContentManager._itemDefs;

	[Obsolete("Use IContentPackProvider instead.")]
	public static event Action<List<ItemDef>> getAdditionalEntries
	{
		add
		{
			LegacyModContentPackProvider.instance.HandleLegacyGetAdditionalEntries("RoR2.ItemCatalog.getAdditionalEntries", value, LegacyModContentPackProvider.instance.registrationContentPack.itemDefs);
		}
		remove
		{
		}
	}

	[SystemInitializer(new Type[] { typeof(ItemTierCatalog) })]
	private static void Init()
	{
		HGXml.Register(delegate(XElement element, ItemIndex[] obj)
		{
			element.Value = string.Join(" ", obj.Select((ItemIndex v) => GetItemDef(v)?.name));
		}, delegate(XElement element, ref ItemIndex[] output)
		{
			output = (from v in element.Value.Split(' ')
				select GetItemDef(FindItemIndex(v))?.itemIndex ?? ItemIndex.None).ToArray();
			return true;
		});
		SetItemDefs(ContentManager.itemDefs);
		SetItemRelationships(ContentManager.itemRelationshipProviders);
		availability.MakeAvailable();
	}

	private static void SetItemDefs(ItemDef[] newItemDefs)
	{
		ItemDef[] itemDefs = ContentManager._itemDefs;
		for (int i = 0; i < itemDefs.Length; i++)
		{
			itemDefs[i].itemIndex = ItemIndex.None;
		}
		ArrayUtils.CloneTo(newItemDefs, ref ContentManager._itemDefs);
		itemNameToIndex.Clear();
		itemOrderBuffers.Clear();
		itemStackArrays.Clear();
		Array.Resize(ref itemNames, newItemDefs.Length);
		for (int j = 0; j < newItemDefs.Length; j++)
		{
			itemNames[j] = newItemDefs[j].name;
		}
		Array.Sort(itemNames, ContentManager._itemDefs, StringComparer.Ordinal);
		for (ItemIndex itemIndex = ItemIndex.Count; (int)itemIndex < ContentManager._itemDefs.Length; itemIndex++)
		{
			ItemDef obj = ContentManager._itemDefs[(int)itemIndex];
			string key = itemNames[(int)itemIndex];
			obj.itemIndex = itemIndex;
			switch (obj.tier)
			{
			case ItemTier.Tier1:
				tier1ItemList.Add(itemIndex);
				break;
			case ItemTier.Tier2:
				tier2ItemList.Add(itemIndex);
				break;
			case ItemTier.Tier3:
				tier3ItemList.Add(itemIndex);
				break;
			case ItemTier.Lunar:
				lunarItemList.Add(itemIndex);
				break;
			}
			itemNameToIndex[key] = itemIndex;
		}
		int num = 23;
		Array.Resize(ref itemIndicesByTag, num);
		ArrayUtils.SetAll(itemIndicesByTag, Array.Empty<ItemIndex>());
		List<ItemIndex>[] array = new List<ItemIndex>[num];
		for (ItemTag itemTag = ItemTag.Any; (int)itemTag < num; itemTag++)
		{
			array[(int)itemTag] = CollectionPool<ItemIndex, List<ItemIndex>>.RentCollection();
		}
		for (ItemIndex itemIndex2 = ItemIndex.Count; (int)itemIndex2 < ContentManager._itemDefs.Length; itemIndex2++)
		{
			ItemTag[] tags = ContentManager._itemDefs[(int)itemIndex2].tags;
			foreach (ItemTag itemTag2 in tags)
			{
				array[(int)itemTag2].Add(itemIndex2);
			}
		}
		for (ItemTag itemTag3 = ItemTag.Any; (int)itemTag3 < num; itemTag3++)
		{
			ref List<ItemIndex> reference = ref array[(int)itemTag3];
			itemIndicesByTag[(int)itemTag3] = reference.ToArray();
			reference = CollectionPool<ItemIndex, List<ItemIndex>>.ReturnCollection(reference);
		}
	}

	private static void SetItemRelationships(ItemRelationshipProvider[] newProviders)
	{
		Dictionary<ItemRelationshipType, HashSet<ItemDef.Pair>> dictionary = new Dictionary<ItemRelationshipType, HashSet<ItemDef.Pair>>();
		foreach (ItemRelationshipProvider itemRelationshipProvider in newProviders)
		{
			if (!dictionary.ContainsKey(itemRelationshipProvider.relationshipType))
			{
				dictionary.Add(itemRelationshipProvider.relationshipType, new HashSet<ItemDef.Pair>());
			}
			dictionary[itemRelationshipProvider.relationshipType].UnionWith(itemRelationshipProvider.relationships);
		}
		itemRelationships.Clear();
		foreach (KeyValuePair<ItemRelationshipType, HashSet<ItemDef.Pair>> item in dictionary)
		{
			IEnumerable<ItemDef.Pair> enumerable = item.Value.Where((ItemDef.Pair pair) => !pair.itemDef1 || !pair.itemDef2);
			foreach (ItemDef.Pair item2 in enumerable)
			{
				Debug.LogError("Trying to define a " + item.Key.name + " relationship between " + item2.itemDef1?.name + " and " + item2.itemDef2?.name + ".");
			}
			item.Value.ExceptWith(enumerable);
			itemRelationships.Add(item.Key, item.Value.ToArray());
		}
	}

	public static ItemIndex[] RequestItemOrderBuffer()
	{
		if (itemOrderBuffers.Count > 0)
		{
			return itemOrderBuffers.Pop();
		}
		return new ItemIndex[itemCount];
	}

	public static void ReturnItemOrderBuffer(ItemIndex[] buffer)
	{
		itemOrderBuffers.Push(buffer);
	}

	public static int[] RequestItemStackArray()
	{
		if (itemStackArrays.Count > 0)
		{
			return itemStackArrays.Pop();
		}
		return new int[itemCount];
	}

	public static void ReturnItemStackArray(int[] itemStackArray)
	{
		if (itemStackArray.Length == itemCount)
		{
			Array.Clear(itemStackArray, 0, itemStackArray.Length);
			itemStackArrays.Push(itemStackArray);
		}
	}

	public static ItemDef GetItemDef(ItemIndex itemIndex)
	{
		return ArrayUtils.GetSafe(ContentManager._itemDefs, (int)itemIndex);
	}

	public static ItemIndex FindItemIndex(string itemName)
	{
		if (itemNameToIndex.TryGetValue(itemName, out var value))
		{
			return value;
		}
		return ItemIndex.None;
	}

	public static T[] GetPerItemBuffer<T>()
	{
		return new T[itemCount];
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsIndexValid(in ItemIndex itemIndex)
	{
		return (uint)itemIndex < (uint)itemCount;
	}

	public static ReadOnlyArray<ItemIndex> GetItemsWithTag(ItemTag itemTag)
	{
		return ArrayUtils.GetSafe(itemIndicesByTag, (int)itemTag, Array.Empty<ItemIndex>());
	}

	public static ReadOnlyArray<ItemDef.Pair> GetItemPairsForRelationship(ItemRelationshipType relationshipType)
	{
		if (itemRelationships.ContainsKey(relationshipType))
		{
			return itemRelationships[relationshipType];
		}
		return Array.Empty<ItemDef.Pair>();
	}

	[ConCommand(commandName = "item_list", flags = ConVarFlags.None, helpText = "Lists internal names of all items registered to the item catalog.")]
	private static void CCEquipmentList(ConCommandArgs args)
	{
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		ItemDef[] itemDefs = ContentManager._itemDefs;
		foreach (ItemDef itemDef in itemDefs)
		{
			string colorHexString = ColorCatalog.GetColorHexString(itemDef.colorIndex);
			stringBuilder.AppendLine("<color=#" + colorHexString + ">" + itemDef.name + "  (" + Language.GetString(itemDef.nameToken) + ")</color>");
		}
		args.Log(stringBuilder.ToString());
		HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
	}

	private static void DefineItems()
	{
	}
}

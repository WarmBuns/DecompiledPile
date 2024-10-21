using System;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/ItemDef")]
public class ItemDef : ScriptableObject
{
	[Serializable]
	public struct Pair : IEquatable<Pair>
	{
		public ItemDef itemDef1;

		public ItemDef itemDef2;

		public override int GetHashCode()
		{
			return itemDef1.GetHashCode() ^ ~itemDef2.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj is Pair)
			{
				return Equals((Pair)obj);
			}
			return false;
		}

		public bool Equals(Pair other)
		{
			if (other.itemDef1 == itemDef1)
			{
				return other.itemDef2 == itemDef2;
			}
			return false;
		}
	}

	private ItemIndex _itemIndex = ItemIndex.None;

	[FormerlySerializedAs("tier")]
	[Obsolete("Replaced by itemTierDef field", false)]
	[SerializeField]
	[Tooltip("Deprecated.  Use itemTierDef instead.")]
	private ItemTier deprecatedTier;

	[SerializeField]
	private ItemTierDef _itemTierDef;

	public string nameToken;

	public string pickupToken;

	public string descriptionToken;

	public string loreToken;

	public UnlockableDef unlockableDef;

	public GameObject pickupModelPrefab;

	public Sprite pickupIconSprite;

	public bool isConsumed;

	public bool hidden;

	public bool canRemove = true;

	public ItemTag[] tags = Array.Empty<ItemTag>();

	public ExpansionDef requiredExpansion;

	public ItemIndex itemIndex
	{
		get
		{
			if (_itemIndex == ItemIndex.None)
			{
				Debug.LogError("ItemDef '" + base.name + "' has an item index of 'None'.  Attempting to fix...");
				_itemIndex = ItemCatalog.FindItemIndex(base.name);
				if (_itemIndex != ItemIndex.None)
				{
					Debug.LogError($"Able to fix ItemDef '{base.name}' (item index = {_itemIndex}).  This is probably because the asset is being duplicated across bundles.");
				}
			}
			return _itemIndex;
		}
		set
		{
			_itemIndex = value;
		}
	}

	public ItemTier tier
	{
		get
		{
			if ((bool)_itemTierDef)
			{
				return _itemTierDef.tier;
			}
			return deprecatedTier;
		}
		set
		{
			_itemTierDef = ItemTierCatalog.GetItemTierDef(value);
		}
	}

	[Obsolete("Get isDroppable from the ItemTierDef instead")]
	public bool inDroppableTier
	{
		get
		{
			ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(tier);
			if ((bool)itemTierDef)
			{
				return itemTierDef.isDroppable;
			}
			return false;
		}
	}

	public Texture pickupIconTexture
	{
		get
		{
			if (!pickupIconSprite)
			{
				return null;
			}
			return pickupIconSprite.texture;
		}
	}

	[Obsolete("Get bgIconTexture from the ItemTierDef instead")]
	public Texture bgIconTexture => ItemTierCatalog.GetItemTierDef(tier)?.bgIconTexture;

	[Obsolete("Get colorIndex from the ItemTierDef instead")]
	public ColorCatalog.ColorIndex colorIndex => ItemTierCatalog.GetItemTierDef(tier)?.colorIndex ?? ColorCatalog.ColorIndex.Unaffordable;

	[Obsolete("Get darkColorIndex from the ItemTierDef instead")]
	public ColorCatalog.ColorIndex darkColorIndex => ItemTierCatalog.GetItemTierDef(tier)?.darkColorIndex ?? ColorCatalog.ColorIndex.Unaffordable;

	public static void AttemptGrant(ref PickupDef.GrantContext context)
	{
		context.body.inventory.GiveItem(PickupCatalog.GetPickupDef(context.controller.pickupIndex)?.itemIndex ?? ItemIndex.None);
		context.body.inventory.GetItemCount(PickupCatalog.GetPickupDef(context.controller.pickupIndex).itemIndex);
		context.shouldDestroy = true;
		context.shouldNotify = true;
	}

	public bool ContainsTag(ItemTag tag)
	{
		if (tag == ItemTag.Any)
		{
			return true;
		}
		return Array.IndexOf(tags, tag) != -1;
	}

	public bool DoesNotContainTag(ItemTag tag)
	{
		return Array.IndexOf(tags, tag) == -1;
	}

	public virtual PickupDef CreatePickupDef()
	{
		ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(tier);
		return new PickupDef
		{
			internalName = "ItemIndex." + base.name,
			itemIndex = itemIndex,
			itemTier = tier,
			displayPrefab = pickupModelPrefab,
			dropletDisplayPrefab = itemTierDef?.dropletDisplayPrefab,
			nameToken = nameToken,
			baseColor = ColorCatalog.GetColor(colorIndex),
			darkColor = ColorCatalog.GetColor(darkColorIndex),
			unlockableDef = unlockableDef,
			interactContextToken = "ITEM_PICKUP_CONTEXT",
			isLunar = (tier == ItemTier.Lunar),
			isBoss = (tier == ItemTier.Boss),
			iconTexture = pickupIconTexture,
			iconSprite = pickupIconSprite,
			attemptGrant = AttemptGrant
		};
	}

	[ContextMenu("Auto Populate Tokens")]
	public void AutoPopulateTokens()
	{
		string arg = base.name.ToUpperInvariant();
		nameToken = $"ITEM_{arg}_NAME";
		pickupToken = $"ITEM_{arg}_PICKUP";
		descriptionToken = $"ITEM_{arg}_DESC";
		loreToken = $"ITEM_{arg}_LORE";
	}

	[ConCommand(commandName = "items_migrate", flags = ConVarFlags.None, helpText = "Generates ItemDef assets from the existing catalog entries.")]
	private static void CCItemsMigrate(ConCommandArgs args)
	{
		for (ItemIndex itemIndex = ItemIndex.Count; (int)itemIndex < ItemCatalog.itemCount; itemIndex++)
		{
			EditorUtil.CopyToScriptableObject<ItemDef, ItemDef>(ItemCatalog.GetItemDef(itemIndex), "Assets/RoR2/Resources/ItemDefs/");
		}
	}
}

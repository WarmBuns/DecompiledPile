using System;
using System.Collections.Generic;
using HG;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/ItemDisplayRuleSet")]
public class ItemDisplayRuleSet : ScriptableObject
{
	[Obsolete]
	[Serializable]
	public struct NamedRuleGroup
	{
		public string name;

		public DisplayRuleGroup displayRuleGroup;
	}

	[Serializable]
	public struct KeyAssetRuleGroup
	{
		public UnityEngine.Object keyAsset;

		public DisplayRuleGroup displayRuleGroup;
	}

	[Obsolete("Use .assetRuleGroups instead.")]
	[SerializeField]
	private NamedRuleGroup[] namedItemRuleGroups = Array.Empty<NamedRuleGroup>();

	[Obsolete("Use .assetRuleGroups instead.")]
	[SerializeField]
	private NamedRuleGroup[] namedEquipmentRuleGroups = Array.Empty<NamedRuleGroup>();

	public KeyAssetRuleGroup[] keyAssetRuleGroups = Array.Empty<KeyAssetRuleGroup>();

	private DisplayRuleGroup[] runtimeItemRuleGroups;

	private DisplayRuleGroup[] runtimeEquipmentRuleGroups;

	private static readonly List<ItemDisplayRuleSet> instancesList = new List<ItemDisplayRuleSet>();

	private static bool runtimeDependenciesReady = false;

	private bool hasObsoleteNamedRuleGroups
	{
		get
		{
			if (namedItemRuleGroups.Length == 0)
			{
				return namedEquipmentRuleGroups.Length != 0;
			}
			return true;
		}
	}

	public bool isEmpty => keyAssetRuleGroups.Length == 0;

	[SystemInitializer(new Type[]
	{
		typeof(ItemCatalog),
		typeof(EquipmentCatalog)
	})]
	private static void Init()
	{
		runtimeDependenciesReady = true;
		for (int i = 0; i < instancesList.Count; i++)
		{
			instancesList[i].GenerateRuntimeValues();
		}
	}

	private void OnEnable()
	{
		instancesList.Add(this);
		if (runtimeDependenciesReady)
		{
			GenerateRuntimeValues();
		}
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	private void OnValidate()
	{
		if (hasObsoleteNamedRuleGroups)
		{
			Debug.LogWarningFormat(this, "ItemDisplayRuleSet \"{0}\" still defines one or more entries in an obsolete format. Run the upgrade from the inspector context menu.", this);
		}
	}

	public void GenerateRuntimeValues()
	{
		runtimeItemRuleGroups = ItemCatalog.GetPerItemBuffer<DisplayRuleGroup>();
		runtimeEquipmentRuleGroups = EquipmentCatalog.GetPerEquipmentBuffer<DisplayRuleGroup>();
		ArrayUtils.SetAll(runtimeItemRuleGroups, in DisplayRuleGroup.empty);
		ArrayUtils.SetAll(runtimeEquipmentRuleGroups, in DisplayRuleGroup.empty);
		for (int i = 0; i < keyAssetRuleGroups.Length; i++)
		{
			ref KeyAssetRuleGroup reference = ref keyAssetRuleGroups[i];
			UnityEngine.Object keyAsset = reference.keyAsset;
			if (!(keyAsset is ItemDef itemDef))
			{
				if (keyAsset is EquipmentDef { equipmentIndex: not EquipmentIndex.None } equipmentDef)
				{
					runtimeEquipmentRuleGroups[(int)equipmentDef.equipmentIndex] = reference.displayRuleGroup;
				}
			}
			else if (itemDef.itemIndex != ItemIndex.None)
			{
				runtimeItemRuleGroups[(int)itemDef.itemIndex] = reference.displayRuleGroup;
			}
		}
	}

	public DisplayRuleGroup FindDisplayRuleGroup(UnityEngine.Object keyAsset)
	{
		if ((bool)keyAsset)
		{
			for (int i = 0; i < keyAssetRuleGroups.Length; i++)
			{
				ref KeyAssetRuleGroup reference = ref keyAssetRuleGroups[i];
				if ((object)reference.keyAsset == keyAsset)
				{
					return reference.displayRuleGroup;
				}
			}
		}
		return DisplayRuleGroup.empty;
	}

	public void SetDisplayRuleGroup(UnityEngine.Object keyAsset, DisplayRuleGroup displayRuleGroup)
	{
		if (!keyAsset)
		{
			return;
		}
		for (int i = 0; i < keyAssetRuleGroups.Length; i++)
		{
			ref KeyAssetRuleGroup reference = ref keyAssetRuleGroups[i];
			if ((object)reference.keyAsset == keyAsset)
			{
				reference.displayRuleGroup = displayRuleGroup;
				return;
			}
		}
		ref KeyAssetRuleGroup[] array = ref keyAssetRuleGroups;
		KeyAssetRuleGroup value = new KeyAssetRuleGroup
		{
			keyAsset = keyAsset,
			displayRuleGroup = displayRuleGroup
		};
		ArrayUtils.ArrayAppend(ref array, in value);
	}

	public DisplayRuleGroup GetItemDisplayRuleGroup(ItemIndex itemIndex)
	{
		return ArrayUtils.GetSafe(runtimeItemRuleGroups, (int)itemIndex, in DisplayRuleGroup.empty);
	}

	public DisplayRuleGroup GetEquipmentDisplayRuleGroup(EquipmentIndex equipmentIndex)
	{
		return ArrayUtils.GetSafe(runtimeEquipmentRuleGroups, (int)equipmentIndex, in DisplayRuleGroup.empty);
	}

	public void Reset()
	{
		keyAssetRuleGroups = Array.Empty<KeyAssetRuleGroup>();
		runtimeItemRuleGroups = Array.Empty<DisplayRuleGroup>();
		runtimeEquipmentRuleGroups = Array.Empty<DisplayRuleGroup>();
	}

	[ContextMenu("Upgrade to keying by asset")]
	public void UpdgradeToAssetKeying()
	{
		_ = Application.isPlaying;
		if (hasObsoleteNamedRuleGroups)
		{
			List<string> list = new List<string>();
			UpgradeNamedRuleGroups(ref namedItemRuleGroups, "ItemDef", (string assetName) => ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex(assetName)), list);
			UpgradeNamedRuleGroups(ref namedEquipmentRuleGroups, "EquipmentDef", (string assetName) => EquipmentCatalog.GetEquipmentDef(EquipmentCatalog.FindEquipmentIndex(assetName)), list);
			if (list.Count > 0)
			{
				Debug.LogWarningFormat("Encountered {0} errors attempting to upgrade ItemDisplayRuleSet \"{1}\":\n{2}", list.Count, base.name, string.Join("\n", list));
			}
			EditorUtil.SetDirty(this);
		}
	}

	private void UpgradeNamedRuleGroups(ref NamedRuleGroup[] namedRuleGroups, string assetTypeName, Func<string, UnityEngine.Object> assetLookupMethod, List<string> failureMessagesList)
	{
		int arraySize = namedRuleGroups.Length;
		for (int i = 0; i < arraySize; i++)
		{
			NamedRuleGroup namedRuleGroup = namedRuleGroups[i];
			UnityEngine.Object @object = assetLookupMethod(namedRuleGroup.name);
			string text = null;
			if (!namedRuleGroup.displayRuleGroup.isEmpty)
			{
				if ((bool)@object)
				{
					if (FindDisplayRuleGroup(@object).isEmpty)
					{
						SetDisplayRuleGroup(@object, namedRuleGroup.displayRuleGroup);
					}
					else
					{
						text = "Conflicts with existing rule group.";
					}
				}
				else
				{
					text = "Named asset not found.";
				}
			}
			if (text != null)
			{
				failureMessagesList.Add(assetTypeName + " \"" + namedRuleGroup.name + "\": " + text);
			}
			else
			{
				ArrayUtils.ArrayRemoveAt(namedRuleGroups, ref arraySize, i);
				i--;
			}
		}
		Array.Resize(ref namedRuleGroups, arraySize);
	}
}

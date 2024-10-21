using System;
using System.Collections.Generic;
using System.Linq;
using HG;
using JetBrains.Annotations;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using RoR2.Skills;
using UnityEngine;

namespace RoR2;

public static class UnlockableCatalog
{
	private static readonly Dictionary<UnlockableIndex, ExpansionDef> unlockableToExpansionTable = new Dictionary<UnlockableIndex, ExpansionDef>();

	private static readonly Dictionary<string, UnlockableDef> nameToDefTable = new Dictionary<string, UnlockableDef>();

	private static UnlockableDef[] indexToDefTable;

	public static ResourceAvailability availability;

	public static int unlockableCount => indexToDefTable.Length;

	[CanBeNull]
	public static UnlockableDef GetUnlockableDef([NotNull] string name)
	{
		if (name == null)
		{
			return null;
		}
		nameToDefTable.TryGetValue(name, out var value);
		return value;
	}

	[CanBeNull]
	public static UnlockableDef GetUnlockableDef(UnlockableIndex index)
	{
		return ArrayUtils.GetSafe(indexToDefTable, (int)index);
	}

	[CanBeNull]
	public static ExpansionDef GetExpansionDefForUnlockable(UnlockableIndex index)
	{
		unlockableToExpansionTable.TryGetValue(index, out var value);
		return value;
	}

	[SystemInitializer(new Type[]
	{
		typeof(SurvivorCatalog),
		typeof(ItemCatalog),
		typeof(EquipmentCatalog),
		typeof(ArtifactCatalog),
		typeof(BodyCatalog)
	})]
	private static void Init()
	{
		SetUnlockableDefs(ContentManager.unlockableDefs);
	}

	private static void SetUnlockableDefs(UnlockableDef[] newUnlockableDefs)
	{
		nameToDefTable.Clear();
		indexToDefTable = ArrayUtils.Clone(newUnlockableDefs);
		for (UnlockableIndex unlockableIndex = (UnlockableIndex)0; (int)unlockableIndex < indexToDefTable.Length; unlockableIndex++)
		{
			UnlockableDef unlockableDef = indexToDefTable[(int)unlockableIndex];
			unlockableDef.index = unlockableIndex;
			nameToDefTable[unlockableDef.cachedName] = unlockableDef;
		}
		GenerateUnlockableMetaData(indexToDefTable);
	}

	private static void TryAddExpansionMapping(UnlockableDef unlockable, ExpansionDef expansion)
	{
		if ((bool)unlockable && (!unlockableToExpansionTable.ContainsKey(unlockable.index) || !unlockableToExpansionTable[unlockable.index]))
		{
			if ((bool)unlockable.requiredExpansion)
			{
				unlockableToExpansionTable[unlockable.index] = unlockable.requiredExpansion;
			}
			else
			{
				unlockableToExpansionTable[unlockable.index] = expansion;
			}
		}
	}

	private static void GenerateUnlockableMetaData(UnlockableDef[] unlockableDefs)
	{
		unlockableToExpansionTable.Clear();
		UnlockableDef[] array = unlockableDefs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].sortScore = int.MaxValue;
		}
		HashSet<UnlockableDef> unlockableDefsSet = new HashSet<UnlockableDef>();
		array = unlockableDefs;
		foreach (UnlockableDef item in array)
		{
			unlockableDefsSet.Add(item);
		}
		int topSortValue = 0;
		foreach (ItemDef item2 in ItemCatalog.allItems.Select(ItemCatalog.GetItemDef).OrderBy(GetItemSortScore))
		{
			AssignUnlockableSortScore(item2.unlockableDef);
			TryAddExpansionMapping(item2.unlockableDef, item2.requiredExpansion);
		}
		foreach (EquipmentDef item3 in EquipmentCatalog.allEquipment.Select(EquipmentCatalog.GetEquipmentDef).OrderBy(GetEquipmentSortScore))
		{
			AssignUnlockableSortScore(item3.unlockableDef);
			TryAddExpansionMapping(item3.unlockableDef, item3.requiredExpansion);
		}
		foreach (SurvivorDef orderedSurvivorDef in SurvivorCatalog.orderedSurvivorDefs)
		{
			AssignUnlockableSortScore(orderedSurvivorDef.unlockableDef);
			BodyIndex bodyIndex = BodyCatalog.FindBodyIndex(orderedSurvivorDef.bodyPrefab);
			if (bodyIndex == BodyIndex.None)
			{
				continue;
			}
			ExpansionDef expansion = null;
			GameObject bodyPrefab = BodyCatalog.GetBodyPrefab(bodyIndex);
			if ((bool)bodyPrefab)
			{
				ExpansionRequirementComponent component = bodyPrefab.GetComponent<ExpansionRequirementComponent>();
				if ((bool)component)
				{
					expansion = component.requiredExpansion;
				}
			}
			TryAddExpansionMapping(orderedSurvivorDef.unlockableDef, expansion);
			GenericSkill[] bodyPrefabSkillSlots = BodyCatalog.GetBodyPrefabSkillSlots(bodyIndex);
			for (int i = 0; i < bodyPrefabSkillSlots.Length; i++)
			{
				SkillFamily skillFamily = bodyPrefabSkillSlots[i].skillFamily;
				if ((bool)skillFamily)
				{
					SkillFamily.Variant[] variants = skillFamily.variants;
					for (int j = 0; j < variants.Length; j++)
					{
						SkillFamily.Variant variant = variants[j];
						AssignUnlockableSortScore(variant.unlockableDef);
						TryAddExpansionMapping(variant.unlockableDef, expansion);
					}
				}
			}
			SkinDef[] bodySkins = BodyCatalog.GetBodySkins(bodyIndex);
			foreach (SkinDef obj in bodySkins)
			{
				AssignUnlockableSortScore(obj.unlockableDef);
				TryAddExpansionMapping(obj.unlockableDef, expansion);
			}
		}
		for (ArtifactIndex artifactIndex = (ArtifactIndex)0; (int)artifactIndex < ArtifactCatalog.artifactCount; artifactIndex++)
		{
			ArtifactDef artifactDef = ArtifactCatalog.GetArtifactDef(artifactIndex);
			AssignUnlockableSortScore(artifactDef.unlockableDef);
			TryAddExpansionMapping(artifactDef.unlockableDef, artifactDef.requiredExpansion);
		}
		void AssignUnlockableSortScore(UnlockableDef unlockableDef)
		{
			if (unlockableDefsSet.Contains(unlockableDef) && unlockableDef.sortScore == int.MaxValue)
			{
				unlockableDef.sortScore = ++topSortValue;
			}
		}
		static int GetEquipmentSortScore(EquipmentDef equipmentDef)
		{
			if (equipmentDef.isLunar)
			{
				return 0;
			}
			return 1;
		}
		static int GetItemSortScore(ItemDef itemDef)
		{
			return itemDef.tier switch
			{
				ItemTier.Tier1 => 1, 
				ItemTier.Tier2 => 2, 
				ItemTier.Tier3 => 3, 
				ItemTier.Lunar => 5, 
				ItemTier.Boss => 4, 
				ItemTier.NoTier => 0, 
				_ => 0, 
			};
		}
	}

	public static int GetUnlockableSortScore(string unlockableName)
	{
		return GetUnlockableDef(unlockableName)?.sortScore ?? 0;
	}
}

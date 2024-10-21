using System;
using JetBrains.Annotations;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Skills;
using UnityEngine;

namespace RoR2.ContentManagement;

public sealed class ContentPack
{
	[NotNull]
	private string _identifier = "UNIDENTIFIED";

	private static Func<GameObject, string> getGameObjectName = (GameObject obj) => obj.name;

	private static Func<Component, string> getComponentName = (Component obj) => obj.gameObject.name;

	private static Func<ScriptableObject, string> getScriptableObjectName = (ScriptableObject obj) => obj.name;

	private static Func<EffectDef, string> getEffectDefName = (EffectDef obj) => obj.prefabName;

	private static Func<Type, string> getTypeName = (Type obj) => obj.FullName;

	[NotNull]
	public NamedAssetCollection<GameObject> bodyPrefabs = new NamedAssetCollection<GameObject>(getGameObjectName);

	[NotNull]
	public NamedAssetCollection<GameObject> masterPrefabs = new NamedAssetCollection<GameObject>(getGameObjectName);

	[NotNull]
	public NamedAssetCollection<GameObject> projectilePrefabs = new NamedAssetCollection<GameObject>(getGameObjectName);

	[NotNull]
	public NamedAssetCollection<GameObject> gameModePrefabs = new NamedAssetCollection<GameObject>(getGameObjectName);

	[NotNull]
	public NamedAssetCollection<GameObject> networkedObjectPrefabs = new NamedAssetCollection<GameObject>(getGameObjectName);

	[NotNull]
	public NamedAssetCollection<SkillDef> skillDefs = new NamedAssetCollection<SkillDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<SkillFamily> skillFamilies = new NamedAssetCollection<SkillFamily>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<SceneDef> sceneDefs = new NamedAssetCollection<SceneDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<ItemDef> itemDefs = new NamedAssetCollection<ItemDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<ItemTierDef> itemTierDefs = new NamedAssetCollection<ItemTierDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<ItemRelationshipProvider> itemRelationshipProviders = new NamedAssetCollection<ItemRelationshipProvider>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<ItemRelationshipType> itemRelationshipTypes = new NamedAssetCollection<ItemRelationshipType>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<EquipmentDef> equipmentDefs = new NamedAssetCollection<EquipmentDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<BuffDef> buffDefs = new NamedAssetCollection<BuffDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<EliteDef> eliteDefs = new NamedAssetCollection<EliteDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<UnlockableDef> unlockableDefs = new NamedAssetCollection<UnlockableDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<SurvivorDef> survivorDefs = new NamedAssetCollection<SurvivorDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<ArtifactDef> artifactDefs = new NamedAssetCollection<ArtifactDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<EffectDef> effectDefs = new NamedAssetCollection<EffectDef>(getEffectDefName);

	[NotNull]
	public NamedAssetCollection<SurfaceDef> surfaceDefs = new NamedAssetCollection<SurfaceDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<NetworkSoundEventDef> networkSoundEventDefs = new NamedAssetCollection<NetworkSoundEventDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<MusicTrackDef> musicTrackDefs = new NamedAssetCollection<MusicTrackDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<GameEndingDef> gameEndingDefs = new NamedAssetCollection<GameEndingDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<EntityStateConfiguration> entityStateConfigurations = new NamedAssetCollection<EntityStateConfiguration>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<ExpansionDef> expansionDefs = new NamedAssetCollection<ExpansionDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<EntitlementDef> entitlementDefs = new NamedAssetCollection<EntitlementDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<MiscPickupDef> miscPickupDefs = new NamedAssetCollection<MiscPickupDef>(getScriptableObjectName);

	[NotNull]
	public NamedAssetCollection<Type> entityStateTypes = new NamedAssetCollection<Type>(getTypeName);

	private object[] assetCollections;

	[NotNull]
	public string identifier
	{
		get
		{
			return _identifier;
		}
		internal set
		{
			_identifier = value ?? throw new ArgumentNullException("'identifier' cannot be null.");
		}
	}

	public ContentPack()
	{
		assetCollections = new object[28]
		{
			bodyPrefabs, masterPrefabs, projectilePrefabs, gameModePrefabs, networkedObjectPrefabs, skillDefs, skillFamilies, sceneDefs, itemDefs, itemTierDefs,
			itemRelationshipProviders, itemRelationshipTypes, equipmentDefs, buffDefs, eliteDefs, unlockableDefs, survivorDefs, artifactDefs, effectDefs, surfaceDefs,
			networkSoundEventDefs, musicTrackDefs, gameEndingDefs, entityStateConfigurations, expansionDefs, entitlementDefs, miscPickupDefs, entityStateTypes
		};
	}

	public bool ValueEquals(ContentPack other)
	{
		return ValueEquals(this, other);
	}

	public static void Copy([NotNull] ContentPack src, [NotNull] ContentPack dest)
	{
		src = src ?? throw new ArgumentNullException("src");
		dest = dest ?? throw new ArgumentNullException("dest");
		dest.identifier = src.identifier;
		src.bodyPrefabs.CopyTo(dest.bodyPrefabs);
		src.masterPrefabs.CopyTo(dest.masterPrefabs);
		src.projectilePrefabs.CopyTo(dest.projectilePrefabs);
		src.gameModePrefabs.CopyTo(dest.gameModePrefabs);
		src.networkedObjectPrefabs.CopyTo(dest.networkedObjectPrefabs);
		src.skillDefs.CopyTo(dest.skillDefs);
		src.skillFamilies.CopyTo(dest.skillFamilies);
		src.sceneDefs.CopyTo(dest.sceneDefs);
		src.itemDefs.CopyTo(dest.itemDefs);
		src.itemTierDefs.CopyTo(dest.itemTierDefs);
		src.itemRelationshipProviders.CopyTo(dest.itemRelationshipProviders);
		src.equipmentDefs.CopyTo(dest.equipmentDefs);
		src.buffDefs.CopyTo(dest.buffDefs);
		src.eliteDefs.CopyTo(dest.eliteDefs);
		src.unlockableDefs.CopyTo(dest.unlockableDefs);
		src.survivorDefs.CopyTo(dest.survivorDefs);
		src.artifactDefs.CopyTo(dest.artifactDefs);
		src.effectDefs.CopyTo(dest.effectDefs);
		src.surfaceDefs.CopyTo(dest.surfaceDefs);
		src.networkSoundEventDefs.CopyTo(dest.networkSoundEventDefs);
		src.musicTrackDefs.CopyTo(dest.musicTrackDefs);
		src.gameEndingDefs.CopyTo(dest.gameEndingDefs);
		src.entityStateConfigurations.CopyTo(dest.entityStateConfigurations);
		src.expansionDefs.CopyTo(dest.expansionDefs);
		src.entitlementDefs.CopyTo(dest.entitlementDefs);
		src.miscPickupDefs.CopyTo(dest.miscPickupDefs);
		src.entityStateTypes.CopyTo(dest.entityStateTypes);
	}

	public static bool ValueEquals([CanBeNull] ContentPack a, [CanBeNull] ContentPack b)
	{
		if (a == null != (b == null))
		{
			return false;
		}
		if (a == null)
		{
			return true;
		}
		if (!a.identifier.Equals(b.identifier, StringComparison.Ordinal))
		{
			return false;
		}
		if (a.bodyPrefabs.Equals(b.bodyPrefabs) && a.masterPrefabs.Equals(b.masterPrefabs) && a.projectilePrefabs.Equals(b.projectilePrefabs) && a.gameModePrefabs.Equals(b.gameModePrefabs) && a.networkedObjectPrefabs.Equals(b.networkedObjectPrefabs) && a.skillDefs.Equals(b.skillDefs) && a.skillFamilies.Equals(b.skillFamilies) && a.sceneDefs.Equals(b.sceneDefs) && a.itemDefs.Equals(b.itemDefs) && a.itemTierDefs.Equals(b.itemTierDefs) && a.itemRelationshipProviders.Equals(b.itemRelationshipProviders) && a.equipmentDefs.Equals(b.equipmentDefs) && a.buffDefs.Equals(b.buffDefs) && a.eliteDefs.Equals(b.eliteDefs) && a.unlockableDefs.Equals(b.unlockableDefs) && a.survivorDefs.Equals(b.survivorDefs) && a.artifactDefs.Equals(b.artifactDefs) && a.effectDefs.Equals(b.effectDefs) && a.surfaceDefs.Equals(b.surfaceDefs) && a.networkSoundEventDefs.Equals(b.networkSoundEventDefs) && a.musicTrackDefs.Equals(b.musicTrackDefs) && a.gameEndingDefs.Equals(b.gameEndingDefs) && a.entityStateConfigurations.Equals(b.entityStateConfigurations) && a.entityStateTypes.Equals(b.entityStateTypes) && a.expansionDefs.Equals(b.expansionDefs) && a.entitlementDefs.Equals(b.entitlementDefs))
		{
			return a.miscPickupDefs.Equals(b.miscPickupDefs);
		}
		return false;
	}

	private NamedAssetCollection FindAssetCollection(string collectionName)
	{
		return collectionName switch
		{
			"bodyPrefabs" => bodyPrefabs, 
			"masterPrefabs" => masterPrefabs, 
			"projectilePrefabs" => projectilePrefabs, 
			"gameModePrefabs" => gameModePrefabs, 
			"networkedObjectPrefabs" => networkedObjectPrefabs, 
			"skillDefs" => skillDefs, 
			"skillFamilies" => skillFamilies, 
			"sceneDefs" => sceneDefs, 
			"itemDefs" => itemDefs, 
			"itemTierDefs" => itemTierDefs, 
			"itemRelationshipProviders" => itemRelationshipProviders, 
			"itemRelationshipTypes" => itemRelationshipTypes, 
			"equipmentDefs" => equipmentDefs, 
			"buffDefs" => buffDefs, 
			"eliteDefs" => eliteDefs, 
			"unlockableDefs" => unlockableDefs, 
			"survivorDefs" => survivorDefs, 
			"artifactDefs" => artifactDefs, 
			"effectDefs" => effectDefs, 
			"surfaceDefs" => surfaceDefs, 
			"networkSoundEventDefs" => networkSoundEventDefs, 
			"musicTrackDefs" => musicTrackDefs, 
			"gameEndingDefs" => gameEndingDefs, 
			"entityStateConfigurations" => entityStateConfigurations, 
			"expansionDefs" => expansionDefs, 
			"entitlementDefs" => entitlementDefs, 
			"miscPickupDefs" => miscPickupDefs, 
			_ => null, 
		};
	}

	public bool FindAsset(string collectionName, string assetName, out object result)
	{
		NamedAssetCollection namedAssetCollection = FindAssetCollection(collectionName);
		if (namedAssetCollection != null)
		{
			return namedAssetCollection.Find(assetName, out result);
		}
		result = null;
		return false;
	}
}

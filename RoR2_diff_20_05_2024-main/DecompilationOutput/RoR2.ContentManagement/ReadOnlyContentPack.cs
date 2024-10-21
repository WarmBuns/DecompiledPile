using System;
using JetBrains.Annotations;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Skills;
using UnityEngine;

namespace RoR2.ContentManagement;

public readonly struct ReadOnlyContentPack
{
	private readonly ContentPack src;

	public bool isValid => src != null;

	public string identifier => src.identifier;

	public ReadOnlyNamedAssetCollection<GameObject> bodyPrefabs => src.bodyPrefabs;

	public ReadOnlyNamedAssetCollection<GameObject> masterPrefabs => src.masterPrefabs;

	public ReadOnlyNamedAssetCollection<GameObject> projectilePrefabs => src.projectilePrefabs;

	public ReadOnlyNamedAssetCollection<GameObject> gameModePrefabs => src.gameModePrefabs;

	public ReadOnlyNamedAssetCollection<GameObject> networkedObjectPrefabs => src.networkedObjectPrefabs;

	public ReadOnlyNamedAssetCollection<SkillDef> skillDefs => src.skillDefs;

	public ReadOnlyNamedAssetCollection<SkillFamily> skillFamilies => src.skillFamilies;

	public ReadOnlyNamedAssetCollection<SceneDef> sceneDefs => src.sceneDefs;

	public ReadOnlyNamedAssetCollection<ItemDef> itemDefs => src.itemDefs;

	public ReadOnlyNamedAssetCollection<ItemTierDef> itemTierDefs => src.itemTierDefs;

	public ReadOnlyNamedAssetCollection<ItemRelationshipProvider> itemRelationshipProviders => src.itemRelationshipProviders;

	public ReadOnlyNamedAssetCollection<ItemRelationshipType> itemRelationshipTypes => src.itemRelationshipTypes;

	public ReadOnlyNamedAssetCollection<EquipmentDef> equipmentDefs => src.equipmentDefs;

	public ReadOnlyNamedAssetCollection<BuffDef> buffDefs => src.buffDefs;

	public ReadOnlyNamedAssetCollection<EliteDef> eliteDefs => src.eliteDefs;

	public ReadOnlyNamedAssetCollection<UnlockableDef> unlockableDefs => src.unlockableDefs;

	public ReadOnlyNamedAssetCollection<SurvivorDef> survivorDefs => src.survivorDefs;

	public ReadOnlyNamedAssetCollection<ArtifactDef> artifactDefs => src.artifactDefs;

	public ReadOnlyNamedAssetCollection<EffectDef> effectDefs => src.effectDefs;

	public ReadOnlyNamedAssetCollection<SurfaceDef> surfaceDefs => src.surfaceDefs;

	public ReadOnlyNamedAssetCollection<NetworkSoundEventDef> networkSoundEventDefs => src.networkSoundEventDefs;

	public ReadOnlyNamedAssetCollection<MusicTrackDef> musicTrackDefs => src.musicTrackDefs;

	public ReadOnlyNamedAssetCollection<GameEndingDef> gameEndingDefs => src.gameEndingDefs;

	public ReadOnlyNamedAssetCollection<EntityStateConfiguration> entityStateConfigurations => src.entityStateConfigurations;

	public ReadOnlyNamedAssetCollection<ExpansionDef> expansionDefs => src.expansionDefs;

	public ReadOnlyNamedAssetCollection<EntitlementDef> entitlementDefs => src.entitlementDefs;

	public ReadOnlyNamedAssetCollection<MiscPickupDef> miscPickupDefs => src.miscPickupDefs;

	public ReadOnlyNamedAssetCollection<Type> entityStateTypes => src.entityStateTypes;

	public ReadOnlyContentPack([NotNull] ContentPack src)
	{
		this.src = src ?? throw new ArgumentNullException("src");
	}

	public bool ValueEquals(in ReadOnlyContentPack other)
	{
		return src.ValueEquals(other.src);
	}

	public static implicit operator ReadOnlyContentPack([NotNull] ContentPack contentPack)
	{
		return new ReadOnlyContentPack(contentPack);
	}

	public bool FindAsset(string collectionName, string assetName, out object result)
	{
		return src.FindAsset(collectionName, assetName, out result);
	}
}

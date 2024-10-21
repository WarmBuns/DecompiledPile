using System;
using System.Collections.Generic;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine.Networking;

namespace RoR2.ContentManagement;

public static class AddressablesLabels
{
	public static readonly string characterBody = "CharacterBody";

	public static readonly string characterMaster = "CharacterMaster";

	public static readonly string projectile = "Projectile";

	public static readonly string gameMode = "GameMode";

	public static readonly string networkedObject = "NetworkedObject";

	public static readonly string effect = "Effect";

	public static readonly string skillFamily = "SkillFamily";

	public static readonly string skillDef = "SkillDef";

	public static readonly string unlockableDef = "UnlockableDef";

	public static readonly string surfaceDef = "SurfaceDef";

	public static readonly string sceneDef = "SceneDef";

	public static readonly string networkSoundEventDef = "NetworkSoundEventDef";

	public static readonly string musicTrackDef = "MusicTrackDef";

	public static readonly string gameEndingDef = "GameEndingDef";

	public static readonly string itemDef = "ItemDef";

	public static readonly string itemTierDef = "ItemTierDef";

	public static readonly string equipmentDef = "EquipmentDef";

	public static readonly string buffDef = "BuffDef";

	public static readonly string eliteDef = "EliteDef";

	public static readonly string survivorDef = "SurvivorDef";

	public static readonly string artifactDef = "ArtifactDef";

	public static readonly string entityStateConfiguration = "EntityStateConfiguration";

	public static readonly string expansionDef = "ExpansionDef";

	public static readonly string entitlementDef = "EntitlementDef";

	public static readonly string itemRelationshipType = "ItemRelationshipType";

	public static readonly string itemRelationshipProvider = "ItemRelationshipProvider";

	public static readonly string miscPickupDef = "MiscPickupDef";

	public static readonly IReadOnlyDictionary<Type, string> componentTypeLabels = new Dictionary<Type, string>
	{
		[typeof(CharacterBody)] = characterBody,
		[typeof(CharacterMaster)] = characterMaster,
		[typeof(ProjectileController)] = projectile,
		[typeof(Run)] = gameMode,
		[typeof(NetworkIdentity)] = networkedObject,
		[typeof(EffectComponent)] = effect
	};

	public static readonly IReadOnlyDictionary<Type, string> assetTypeLabels = new Dictionary<Type, string>
	{
		[typeof(SkillFamily)] = skillFamily,
		[typeof(SkillDef)] = skillDef,
		[typeof(UnlockableDef)] = unlockableDef,
		[typeof(SurfaceDef)] = surfaceDef,
		[typeof(SceneDef)] = sceneDef,
		[typeof(NetworkSoundEventDef)] = networkSoundEventDef,
		[typeof(MusicTrackDef)] = musicTrackDef,
		[typeof(GameEndingDef)] = gameEndingDef,
		[typeof(ItemDef)] = itemDef,
		[typeof(ItemTierDef)] = itemTierDef,
		[typeof(EquipmentDef)] = equipmentDef,
		[typeof(BuffDef)] = buffDef,
		[typeof(EliteDef)] = eliteDef,
		[typeof(SurvivorDef)] = survivorDef,
		[typeof(ArtifactDef)] = artifactDef,
		[typeof(EntityStateConfiguration)] = entityStateConfiguration,
		[typeof(ExpansionDef)] = expansionDef,
		[typeof(EntitlementDef)] = entitlementDef,
		[typeof(ItemRelationshipProvider)] = itemRelationshipProvider,
		[typeof(ItemRelationshipType)] = itemRelationshipType,
		[typeof(MiscPickupDef)] = miscPickupDef
	};
}

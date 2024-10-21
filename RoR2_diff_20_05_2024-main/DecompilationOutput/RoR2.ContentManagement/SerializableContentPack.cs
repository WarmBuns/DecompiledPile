using System;
using System.Collections.Generic;
using System.Linq;
using EntityStates;
using RoR2.Skills;
using UnityEngine;

namespace RoR2.ContentManagement;

[CreateAssetMenu(menuName = "RoR2/SerializableContentPack")]
public class SerializableContentPack : ScriptableObject
{
	public GameObject[] bodyPrefabs = Array.Empty<GameObject>();

	public GameObject[] masterPrefabs = Array.Empty<GameObject>();

	public GameObject[] projectilePrefabs = Array.Empty<GameObject>();

	public GameObject[] gameModePrefabs = Array.Empty<GameObject>();

	public GameObject[] networkedObjectPrefabs = Array.Empty<GameObject>();

	public SkillDef[] skillDefs = Array.Empty<SkillDef>();

	public SkillFamily[] skillFamilies = Array.Empty<SkillFamily>();

	public SceneDef[] sceneDefs = Array.Empty<SceneDef>();

	public ItemDef[] itemDefs = Array.Empty<ItemDef>();

	public EquipmentDef[] equipmentDefs = Array.Empty<EquipmentDef>();

	public BuffDef[] buffDefs = Array.Empty<BuffDef>();

	public EliteDef[] eliteDefs = Array.Empty<EliteDef>();

	public UnlockableDef[] unlockableDefs = Array.Empty<UnlockableDef>();

	public SurvivorDef[] survivorDefs = Array.Empty<SurvivorDef>();

	public ArtifactDef[] artifactDefs = Array.Empty<ArtifactDef>();

	public GameObject[] effectDefs = Array.Empty<GameObject>();

	public SurfaceDef[] surfaceDefs = Array.Empty<SurfaceDef>();

	public NetworkSoundEventDef[] networkSoundEventDefs = Array.Empty<NetworkSoundEventDef>();

	public MusicTrackDef[] musicTrackDefs = Array.Empty<MusicTrackDef>();

	public GameEndingDef[] gameEndingDefs = Array.Empty<GameEndingDef>();

	public EntityStateConfiguration[] entityStateConfigurations = Array.Empty<EntityStateConfiguration>();

	public SerializableEntityStateType[] entityStateTypes = Array.Empty<SerializableEntityStateType>();

	public ContentPack CreateV1_1_1_4_ContentPack()
	{
		ContentPack contentPack = new ContentPack();
		contentPack.bodyPrefabs.Add(bodyPrefabs);
		contentPack.masterPrefabs.Add(masterPrefabs);
		contentPack.projectilePrefabs.Add(projectilePrefabs);
		contentPack.gameModePrefabs.Add(gameModePrefabs);
		contentPack.networkedObjectPrefabs.Add(networkedObjectPrefabs);
		contentPack.skillDefs.Add(skillDefs);
		contentPack.skillFamilies.Add(skillFamilies);
		contentPack.sceneDefs.Add(sceneDefs);
		contentPack.itemDefs.Add(itemDefs);
		contentPack.equipmentDefs.Add(equipmentDefs);
		contentPack.buffDefs.Add(buffDefs);
		contentPack.eliteDefs.Add(eliteDefs);
		contentPack.unlockableDefs.Add(unlockableDefs);
		contentPack.survivorDefs.Add(survivorDefs);
		contentPack.artifactDefs.Add(artifactDefs);
		contentPack.effectDefs.Add(effectDefs.Select((GameObject asset) => new EffectDef(asset)).ToArray());
		contentPack.surfaceDefs.Add(surfaceDefs);
		contentPack.networkSoundEventDefs.Add(networkSoundEventDefs);
		contentPack.musicTrackDefs.Add(musicTrackDefs);
		contentPack.gameEndingDefs.Add(gameEndingDefs);
		contentPack.entityStateConfigurations.Add(entityStateConfigurations);
		List<Type> list = new List<Type>();
		for (int i = 0; i < entityStateTypes.Length; i++)
		{
			Type stateType = entityStateTypes[i].stateType;
			if (stateType != null)
			{
				list.Add(stateType);
				continue;
			}
			Debug.LogWarning("SerializableContentPack \"" + base.name + "\" could not resolve type with name \"" + entityStateTypes[i].typeName + "\". The type will not be available in the content pack.");
		}
		contentPack.entityStateTypes.Add(list.ToArray());
		return contentPack;
	}

	public virtual ContentPack CreateContentPack()
	{
		return CreateV1_1_1_4_ContentPack();
	}
}

using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/SpawnCards/MultiCharacterSpawnCard")]
public class MultiCharacterSpawnCard : CharacterSpawnCard
{
	public GameObject[] masterPrefabs;

	protected override void Spawn(Vector3 position, Quaternion rotation, DirectorSpawnRequest directorSpawnRequest, ref SpawnResult result)
	{
		prefab = masterPrefabs[(int)(directorSpawnRequest.rng.nextNormalizedFloat * (float)masterPrefabs.Length)];
		base.Spawn(position, rotation, directorSpawnRequest, ref result);
	}
}

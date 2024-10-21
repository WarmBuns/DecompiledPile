using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/SpawnCards/BodySpawnCard")]
public class BodySpawnCard : SpawnCard
{
	protected override void Spawn(Vector3 position, Quaternion rotation, DirectorSpawnRequest directorSpawnRequest, ref SpawnResult result)
	{
		Vector3 position2 = position;
		position2.y += Util.GetBodyPrefabFootOffset(prefab);
		GameObject gameObject = Object.Instantiate(prefab, position2, rotation);
		NetworkServer.Spawn(gameObject);
		result.spawnedInstance = gameObject;
		result.success = true;
	}
}

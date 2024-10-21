using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/GameObjectFactory")]
public class GameObjectFactory : ScriptableObject
{
	public bool clearOnPerformInstantiate = true;

	private GameObject prefab;

	private Vector3 spawnPosition;

	private Quaternion spawnRotation;

	private bool isNetworkPrefab;

	private AnimationClip spawnModificationsClip;

	public void Reset()
	{
		Clear();
		clearOnPerformInstantiate = true;
	}

	public void Clear()
	{
		prefab = null;
		spawnPosition = Vector3.zero;
		spawnRotation = Quaternion.identity;
		isNetworkPrefab = false;
		spawnModificationsClip = null;
	}

	public void SetPrefab(GameObject newPrefab)
	{
		prefab = newPrefab;
		isNetworkPrefab = prefab?.GetComponent<NetworkIdentity>();
	}

	public void SetSpawnLocation(Transform newSpawnLocation)
	{
		spawnPosition = newSpawnLocation.position;
		spawnRotation = newSpawnLocation.rotation;
	}

	public void SetSpawnModificationsClip(AnimationClip newSpawnModificationsClip)
	{
		spawnModificationsClip = newSpawnModificationsClip;
	}

	public void PerformInstantiate()
	{
		if ((bool)prefab)
		{
			GameObject gameObject = Object.Instantiate(prefab, spawnPosition, spawnRotation);
			if ((bool)spawnModificationsClip)
			{
				Animator obj = gameObject.AddComponent<Animator>();
				spawnModificationsClip.SampleAnimation(gameObject, 0f);
				Object.Destroy(obj);
			}
			gameObject.SetActive(value: true);
			if (isNetworkPrefab && NetworkServer.active)
			{
				NetworkServer.Spawn(gameObject);
			}
			if (clearOnPerformInstantiate)
			{
				Clear();
			}
		}
	}
}

using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class InstantiatePrefabBehavior : MonoBehaviour
{
	[Tooltip("The prefab to instantiate.")]
	public GameObject prefab;

	[Tooltip("The object upon which the prefab will be positioned.")]
	public Transform targetTransform;

	[Tooltip("The transform upon which to instantiate the prefab.")]
	public bool copyTargetRotation;

	[Tooltip("Whether or not to parent the instantiated prefab to the specified transform.")]
	public bool parentToTarget;

	[Tooltip("Whether or not this is a networked prefab. If so, this will only run on the server, and will be spawned over the network.")]
	public bool networkedPrefab;

	[Tooltip("Whether or not to instantiate this prefab. If so, this will only run on the server, and will be spawned over the network.")]
	public bool instantiateOnStart = true;

	[Tooltip("Whether or not to force this prefab to be active, even if the prefab itself is inactive.")]
	public bool forceActivate;

	public void Start()
	{
		if (instantiateOnStart)
		{
			InstantiatePrefab();
		}
	}

	public void InstantiatePrefab()
	{
		if (!networkedPrefab || NetworkServer.active)
		{
			Vector3 position = (targetTransform ? targetTransform.position : Vector3.zero);
			Quaternion rotation = (copyTargetRotation ? targetTransform.rotation : Quaternion.identity);
			Transform parent = (parentToTarget ? targetTransform : null);
			GameObject gameObject = Object.Instantiate(prefab, position, rotation, parent);
			if (forceActivate)
			{
				gameObject.SetActive(value: true);
			}
			if (networkedPrefab)
			{
				NetworkServer.Spawn(gameObject);
			}
		}
	}
}

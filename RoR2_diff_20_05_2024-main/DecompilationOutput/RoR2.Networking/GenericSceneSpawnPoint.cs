using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Networking;

public class GenericSceneSpawnPoint : MonoBehaviour
{
	public GameObject networkedObjectPrefab;

	private void Start()
	{
		if (NetworkServer.active)
		{
			NetworkServer.Spawn(Object.Instantiate(networkedObjectPrefab, base.transform.position, base.transform.rotation));
			base.gameObject.SetActive(value: false);
		}
		else
		{
			base.gameObject.SetActive(value: false);
		}
	}
}

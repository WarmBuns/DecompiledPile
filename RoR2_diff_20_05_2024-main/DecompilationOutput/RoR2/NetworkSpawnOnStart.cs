using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class NetworkSpawnOnStart : MonoBehaviour
{
	private void Start()
	{
		if (NetworkServer.active)
		{
			NetworkServer.Spawn(base.gameObject);
		}
	}
}

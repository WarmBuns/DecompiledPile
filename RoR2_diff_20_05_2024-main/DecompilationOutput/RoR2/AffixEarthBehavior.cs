using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2;

public class AffixEarthBehavior : CharacterBody.ItemBehavior
{
	private const float baseOrbitDegreesPerSecond = 90f;

	private const float baseOrbitRadius = 3f;

	private const string projectilePath = "Prefabs/Projectiles/AffixEarthProjectile";

	private GameObject affixEarthAttachment;

	private GameObject projectilePrefab;

	private int maxProjectiles;

	private static GameObject attachmentPrefab;

	[InitDuringStartup]
	private static void Init()
	{
		AsyncOperationHandle<GameObject> asyncOperationHandle = Addressables.LoadAssetAsync<GameObject>("112bf5913df2135478a9785e0bc18477");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
		{
			attachmentPrefab = x.Result;
		};
	}

	private void Start()
	{
	}

	private void FixedUpdate()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		bool flag = stack > 0;
		if ((bool)affixEarthAttachment != flag)
		{
			if (flag)
			{
				affixEarthAttachment = Object.Instantiate(attachmentPrefab);
				affixEarthAttachment.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(body.gameObject);
			}
			else
			{
				Object.Destroy(affixEarthAttachment);
				affixEarthAttachment = null;
			}
		}
	}

	private void OnDisable()
	{
		if ((bool)affixEarthAttachment)
		{
			Object.Destroy(affixEarthAttachment);
		}
	}
}

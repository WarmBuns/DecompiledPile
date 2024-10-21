using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Items;

public static class CrippleWardOnLevelManager
{
	private static GameObject wardPrefab;

	[SystemInitializer(new Type[] { typeof(ItemCatalog) })]
	private static void Init()
	{
		Run.onRunAmbientLevelUp += onRunAmbientLevelUp;
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/NetworkedObjects/CrippleWard", delegate(GameObject operationResult)
		{
			wardPrefab = operationResult;
		});
	}

	private static void onRunAmbientLevelUp(Run run)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		foreach (CharacterMaster readOnlyInstances in CharacterMaster.readOnlyInstancesList)
		{
			int itemCount = readOnlyInstances.inventory.GetItemCount(RoR2Content.Items.CrippleWardOnLevel);
			if (itemCount > 0)
			{
				CharacterBody body = readOnlyInstances.GetBody();
				if ((bool)body)
				{
					GameObject gameObject = UnityEngine.Object.Instantiate(wardPrefab, body.transform.position, Quaternion.identity);
					gameObject.GetComponent<BuffWard>().Networkradius = 8f + 8f * (float)itemCount;
					NetworkServer.Spawn(gameObject);
				}
			}
		}
	}
}

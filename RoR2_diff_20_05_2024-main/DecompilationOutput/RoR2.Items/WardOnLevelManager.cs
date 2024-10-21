using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Items;

public static class WardOnLevelManager
{
	private static GameObject wardPrefab;

	[SystemInitializer(new Type[] { typeof(ItemCatalog) })]
	private static void Init()
	{
		GlobalEventManager.onCharacterLevelUp += OnCharacterLevelUp;
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/NetworkedObjects/WarbannerWard", delegate(GameObject operationResult)
		{
			wardPrefab = operationResult;
		});
	}

	private static void OnCharacterLevelUp(CharacterBody characterBody)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		Inventory inventory = characterBody.inventory;
		if ((bool)inventory)
		{
			int itemCount = inventory.GetItemCount(RoR2Content.Items.WardOnLevel);
			if (itemCount > 0)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate(wardPrefab, characterBody.transform.position, Quaternion.identity);
				gameObject.GetComponent<TeamFilter>().teamIndex = characterBody.teamComponent.teamIndex;
				gameObject.GetComponent<BuffWard>().Networkradius = 8f + 8f * (float)itemCount;
				NetworkServer.Spawn(gameObject);
			}
		}
	}
}

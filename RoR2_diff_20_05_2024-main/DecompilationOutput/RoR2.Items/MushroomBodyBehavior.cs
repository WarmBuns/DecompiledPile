using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2.Items;

public class MushroomBodyBehavior : BaseItemBodyBehavior
{
	private static GameObject mushroomWardPrefab;

	private const float baseHealFractionPerSecond = 0.045f;

	private const float healFractionPerSecondPerStack = 0.0225f;

	private GameObject mushroomWardGameObject;

	private HealingWard mushroomHealingWard;

	private TeamFilter mushroomWardTeamFilter;

	[ItemDefAssociation(useOnServer = true, useOnClient = false)]
	private static ItemDef GetItemDef()
	{
		return RoR2Content.Items.Mushroom;
	}

	[InitDuringStartup]
	private static void Init()
	{
		AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/NetworkedObjects/MushroomWard");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
		{
			mushroomWardPrefab = x.Result;
		};
	}

	private void FixedUpdate()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		int num = stack;
		bool flag = num > 0 && base.body.GetNotMoving();
		float networkradius = base.body.radius + 1.5f + 1.5f * (float)num;
		if ((bool)mushroomWardGameObject != flag)
		{
			if (flag)
			{
				mushroomWardGameObject = Object.Instantiate(mushroomWardPrefab, base.body.footPosition, Quaternion.identity);
				mushroomWardTeamFilter = mushroomWardGameObject.GetComponent<TeamFilter>();
				mushroomHealingWard = mushroomWardGameObject.GetComponent<HealingWard>();
				NetworkServer.Spawn(mushroomWardGameObject);
			}
			else
			{
				Object.Destroy(mushroomWardGameObject);
				mushroomWardGameObject = null;
			}
		}
		if ((bool)mushroomHealingWard)
		{
			mushroomHealingWard.interval = 0.25f;
			mushroomHealingWard.healFraction = (0.045f + 0.0225f * (float)(num - 1)) * mushroomHealingWard.interval;
			mushroomHealingWard.healPoints = 0f;
			mushroomHealingWard.Networkradius = networkradius;
		}
		if ((bool)mushroomWardTeamFilter)
		{
			mushroomWardTeamFilter.teamIndex = base.body.teamComponent.teamIndex;
		}
	}

	private void OnDisable()
	{
		if ((bool)mushroomWardGameObject)
		{
			Object.Destroy(mushroomWardGameObject);
		}
	}
}

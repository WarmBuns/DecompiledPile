using UnityEngine;

namespace RoR2.Items;

public class BeetleGlandBodyBehavior : BaseItemBodyBehavior
{
	private static readonly float timeBetweenGuardResummons = 30f;

	private static readonly float timeBetweenGuardRetryResummons = 1f;

	private float guardResummonCooldown;

	[ItemDefAssociation(useOnServer = true, useOnClient = false)]
	private static ItemDef GetItemDef()
	{
		return RoR2Content.Items.BeetleGland;
	}

	private void FixedUpdate()
	{
		int num = stack;
		CharacterMaster bodyMaster = base.body.master;
		if (!bodyMaster)
		{
			return;
		}
		int deployableCount = bodyMaster.GetDeployableCount(DeployableSlot.BeetleGuardAlly);
		if (deployableCount >= num)
		{
			return;
		}
		guardResummonCooldown -= Time.fixedDeltaTime;
		if (guardResummonCooldown <= 0f)
		{
			DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/CharacterSpawnCards/cscBeetleGuardAlly"), new DirectorPlacementRule
			{
				placementMode = DirectorPlacementRule.PlacementMode.Approximate,
				minDistance = 3f,
				maxDistance = 40f,
				spawnOnTarget = base.transform
			}, RoR2Application.rng);
			directorSpawnRequest.summonerBodyObject = base.gameObject;
			directorSpawnRequest.onSpawnedServer = OnGuardMasterSpawned;
			DirectorCore.instance.TrySpawnObject(directorSpawnRequest);
			if (deployableCount < num)
			{
				guardResummonCooldown = timeBetweenGuardRetryResummons;
			}
			else
			{
				guardResummonCooldown = timeBetweenGuardResummons;
			}
		}
		void OnGuardMasterSpawned(SpawnCard.SpawnResult spawnResult)
		{
			GameObject spawnedInstance = spawnResult.spawnedInstance;
			if ((bool)spawnedInstance)
			{
				CharacterMaster component = spawnedInstance.GetComponent<CharacterMaster>();
				if ((bool)component)
				{
					component.inventory.GiveItem(RoR2Content.Items.BoostDamage, 30);
					component.inventory.GiveItem(RoR2Content.Items.BoostHp, 10);
					Deployable component2 = component.GetComponent<Deployable>();
					if ((bool)component2)
					{
						bodyMaster.AddDeployable(component2, DeployableSlot.BeetleGuardAlly);
					}
				}
			}
		}
	}
}

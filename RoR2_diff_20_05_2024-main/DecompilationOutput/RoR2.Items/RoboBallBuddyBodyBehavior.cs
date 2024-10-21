using UnityEngine;

namespace RoR2.Items;

public class RoboBallBuddyBodyBehavior : BaseItemBodyBehavior
{
	private class InventorySync : MonoBehaviour
	{
		public Inventory srcInventory;

		public Inventory destInventory;

		private int granted;

		private void FixedUpdate()
		{
			if ((bool)srcInventory && (bool)destInventory)
			{
				int itemCount = srcInventory.GetItemCount(RoR2Content.Items.RoboBallBuddy);
				int num = itemCount - granted;
				if (num != 0)
				{
					destInventory.GiveItem(RoR2Content.Items.TeamSizeDamageBonus, num);
					granted = itemCount;
				}
			}
		}
	}

	private DeployableMinionSpawner redBuddySpawner;

	private DeployableMinionSpawner greenBuddySpawner;

	[ItemDefAssociation(useOnServer = true, useOnClient = false)]
	private static ItemDef GetItemDef()
	{
		return RoR2Content.Items.RoboBallBuddy;
	}

	private void FixedUpdate()
	{
		if (redBuddySpawner == null && base.isActiveAndEnabled)
		{
			CreateSpawners();
		}
	}

	private void OnDisable()
	{
		DestroySpawners();
	}

	private void CreateSpawners()
	{
		Xoroshiro128Plus rng = new Xoroshiro128Plus(Run.instance.seed ^ (ulong)GetInstanceID());
		CreateSpawner(ref redBuddySpawner, DeployableSlot.RoboBallRedBuddy, LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/CharacterSpawnCards/cscRoboBallRedBuddy"));
		CreateSpawner(ref greenBuddySpawner, DeployableSlot.RoboBallGreenBuddy, LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/CharacterSpawnCards/cscRoboBallGreenBuddy"));
		void CreateSpawner(ref DeployableMinionSpawner buddySpawner, DeployableSlot deployableSlot, SpawnCard spawnCard)
		{
			buddySpawner = new DeployableMinionSpawner(base.body.master, deployableSlot, rng)
			{
				respawnInterval = 30f,
				spawnCard = spawnCard
			};
			buddySpawner.onMinionSpawnedServer += OnMinionSpawnedServer;
		}
	}

	private void DestroySpawners()
	{
		redBuddySpawner?.Dispose();
		redBuddySpawner = null;
		greenBuddySpawner?.Dispose();
		greenBuddySpawner = null;
	}

	private void OnMinionSpawnedServer(SpawnCard.SpawnResult spawnResult)
	{
		GameObject spawnedInstance = spawnResult.spawnedInstance;
		if (!spawnedInstance)
		{
			return;
		}
		CharacterMaster component = spawnedInstance.GetComponent<CharacterMaster>();
		if ((bool)component)
		{
			Inventory inventory = base.body.inventory;
			Inventory inventory2 = component.inventory;
			if ((bool)inventory)
			{
				InventorySync inventorySync = spawnedInstance.AddComponent<InventorySync>();
				inventorySync.srcInventory = inventory;
				inventorySync.destInventory = inventory2;
			}
		}
	}
}

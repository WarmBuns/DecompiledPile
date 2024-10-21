using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Artifacts;

public static class MonsterTeamGainsItemsArtifactManager
{
	private static GameObject networkedInventoryPrefab;

	private static Inventory monsterTeamInventory;

	private static readonly Xoroshiro128Plus treasureRng = new Xoroshiro128Plus(0uL);

	private static PickupDropTable[] dropPattern;

	private static int currentItemIterator = 0;

	private static ArtifactDef myArtifact => RoR2Content.Artifacts.monsterTeamGainsItemsArtifactDef;

	[SystemInitializer(new Type[]
	{
		typeof(ArtifactCatalog),
		typeof(ItemCatalog)
	})]
	private static void Init()
	{
		RunArtifactManager.onArtifactEnabledGlobal += OnArtifactEnabled;
		RunArtifactManager.onArtifactDisabledGlobal += OnArtifactDisabled;
		Run.onRunStartGlobal += OnRunStartGlobal;
		Run.onRunDestroyGlobal += OnRunDestroyGlobal;
		Stage.onServerStageBegin += OnServerStageBegin;
		SceneDirector.onPrePopulateSceneServer += OnPrePopulateSceneServer;
		LegacyResourcesAPI.LoadAsyncCallback("Prefabs/NetworkedObjects/MonsterTeamGainsItemsArtifactInventory", delegate(GameObject operationResult)
		{
			networkedInventoryPrefab = operationResult;
		});
		PickupDropTable pickupDropTable = LegacyResourcesAPI.Load<PickupDropTable>("DropTables/dtMonsterTeamTier1Item");
		PickupDropTable pickupDropTable2 = LegacyResourcesAPI.Load<PickupDropTable>("DropTables/dtMonsterTeamTier2Item");
		PickupDropTable pickupDropTable3 = LegacyResourcesAPI.Load<PickupDropTable>("DropTables/dtMonsterTeamTier3Item");
		dropPattern = new PickupDropTable[5] { pickupDropTable, pickupDropTable, pickupDropTable2, pickupDropTable2, pickupDropTable3 };
	}

	private static void OnRunStartGlobal(Run run)
	{
		if (NetworkServer.active)
		{
			currentItemIterator = 0;
			treasureRng.ResetSeed(run.seed);
			monsterTeamInventory = UnityEngine.Object.Instantiate(networkedInventoryPrefab).GetComponent<Inventory>();
			monsterTeamInventory.GetComponent<TeamFilter>().teamIndex = TeamIndex.Monster;
			NetworkServer.Spawn(monsterTeamInventory.gameObject);
			EnsureMonsterItemCountMatchesStageCount();
		}
	}

	private static void OnRunDestroyGlobal(Run run)
	{
		treasureRng.ResetSeed(0uL);
		if ((bool)monsterTeamInventory)
		{
			NetworkServer.Destroy(monsterTeamInventory.gameObject);
		}
		monsterTeamInventory = null;
	}

	private static void GrantMonsterTeamItem()
	{
		PickupDropTable pickupDropTable = dropPattern[currentItemIterator++ % dropPattern.Length];
		if (!pickupDropTable)
		{
			return;
		}
		PickupIndex pickupIndex = pickupDropTable.GenerateDrop(treasureRng);
		if (pickupIndex != PickupIndex.none)
		{
			PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
			if (pickupDef != null)
			{
				monsterTeamInventory.GiveItem(pickupDef.itemIndex);
			}
		}
	}

	private static void EnsureMonsterItemCountMatchesStageCount()
	{
		EnsureMonsterTeamItemCount(Run.instance.stageClearCount + 1);
	}

	private static void OnServerStageBegin(Stage stage)
	{
		EnsureMonsterItemCountMatchesStageCount();
	}

	private static void OnPrePopulateSceneServer(SceneDirector sceneDirector)
	{
		EnsureMonsterItemCountMatchesStageCount();
	}

	private static void EnsureMonsterTeamItemCount(int itemCount)
	{
		while (currentItemIterator < itemCount)
		{
			GrantMonsterTeamItem();
		}
	}

	private static void OnArtifactEnabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		if (!(artifactDef != myArtifact) && NetworkServer.active)
		{
			SpawnCard.onSpawnedServerGlobal += OnServerCardSpawnedGlobal;
		}
	}

	private static void OnArtifactDisabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
	{
		if (!(artifactDef != myArtifact))
		{
			SpawnCard.onSpawnedServerGlobal -= OnServerCardSpawnedGlobal;
		}
	}

	private static void OnServerCardSpawnedGlobal(SpawnCard.SpawnResult spawnResult)
	{
		CharacterMaster characterMaster = (spawnResult.spawnedInstance ? spawnResult.spawnedInstance.GetComponent<CharacterMaster>() : null);
		if ((bool)characterMaster && characterMaster.teamIndex == TeamIndex.Monster)
		{
			characterMaster.inventory.AddItemsFrom(monsterTeamInventory);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2.Stats;

internal class StatManager
{
	private struct DamageEvent
	{
		[CanBeNull]
		public CharacterMaster attackerMaster;

		public BodyIndex attackerBodyIndex;

		[CanBeNull]
		public CharacterMaster attackerOwnerMaster;

		public BodyIndex attackerOwnerBodyIndex;

		[CanBeNull]
		public CharacterMaster victimMaster;

		public BodyIndex victimBodyIndex;

		public bool victimIsElite;

		public float damageDealt;

		public DotController.DotIndex dotType;
	}

	private struct DeathEvent
	{
		public DamageReport damageReport;

		public bool victimWasBurning;
	}

	private struct HealingEvent
	{
		[CanBeNull]
		public GameObject healee;

		public float healAmount;
	}

	private struct GoldEvent
	{
		[CanBeNull]
		public CharacterMaster characterMaster;

		public ulong amount;
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct PurchaseStatEvent
	{
	}

	public struct CharacterUpdateEvent
	{
		public PlayerStatsComponent statsComponent;

		public float additionalDistanceTraveled;

		public float additionalTimeAlive;

		public int level;

		public float runTime;
	}

	private struct ItemCollectedEvent
	{
		[CanBeNull]
		public Inventory inventory;

		public ItemIndex itemIndex;

		public int quantity;

		public int newCount;
	}

	private static BodyIndex crocoBodyIndex = BodyIndex.None;

	private static readonly Queue<DamageEvent> damageEvents = new Queue<DamageEvent>();

	private static readonly Queue<DeathEvent> deathEvents = new Queue<DeathEvent>();

	private static readonly Queue<HealingEvent> healingEvents = new Queue<HealingEvent>();

	private static readonly Queue<GoldEvent> goldCollectedEvents = new Queue<GoldEvent>();

	private static readonly Queue<PurchaseStatEvent> purchaseStatEvents = new Queue<PurchaseStatEvent>();

	private static readonly Queue<CharacterUpdateEvent> characterUpdateEvents = new Queue<CharacterUpdateEvent>();

	private static readonly Queue<ItemCollectedEvent> itemCollectedEvents = new Queue<ItemCollectedEvent>();

	[SystemInitializer(new Type[]
	{
		typeof(BodyCatalog),
		typeof(ItemCatalog),
		typeof(EquipmentCatalog),
		typeof(PickupCatalog)
	})]
	private static void Init()
	{
		GlobalEventManager.onServerDamageDealt += OnDamageDealt;
		GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
		GlobalEventManager.onServerCharacterExecuted += OnCharacterExecute;
		HealthComponent.onCharacterHealServer += OnCharacterHeal;
		Run.onPlayerFirstCreatedServer += OnPlayerFirstCreatedServer;
		Run.onServerGameOver += OnServerGameOver;
		Stage.onServerStageComplete += OnServerStageComplete;
		Stage.onServerStageBegin += OnServerStageBegin;
		Inventory.onServerItemGiven += OnServerItemGiven;
		RoR2Application.onFixedUpdate += ProcessEvents;
		EquipmentSlot.onServerEquipmentActivated += OnEquipmentActivated;
		InfiniteTowerRun.onWaveInitialized += OnInfiniteTowerWaveInitialized;
		crocoBodyIndex = BodyCatalog.FindBodyIndex("CrocoBody");
	}

	private static void OnInfiniteTowerWaveInitialized(InfiniteTowerWaveController waveController)
	{
		ulong statValue = (ulong)((Run.instance as InfiniteTowerRun)?.waveIndex ?? 0);
		foreach (PlayerStatsComponent instances in PlayerStatsComponent.instancesList)
		{
			if (!instances.playerCharacterMasterController.isConnected)
			{
				continue;
			}
			PerBodyStatDef perBodyStatDef = null;
			switch (Run.instance.selectedDifficulty)
			{
			case DifficultyIndex.Easy:
				perBodyStatDef = PerBodyStatDef.highestInfiniteTowerWaveReachedEasy;
				break;
			case DifficultyIndex.Normal:
				perBodyStatDef = PerBodyStatDef.highestInfiniteTowerWaveReachedNormal;
				break;
			case DifficultyIndex.Hard:
				perBodyStatDef = PerBodyStatDef.highestInfiniteTowerWaveReachedHard;
				break;
			}
			StatSheet currentStats = instances.currentStats;
			currentStats.PushStatValue(StatDef.highestInfiniteTowerWaveReached, statValue);
			if (perBodyStatDef != null)
			{
				CharacterBody body = instances.characterMaster.GetBody();
				if ((bool)body)
				{
					string bodyName = BodyCatalog.GetBodyName(body.bodyIndex);
					currentStats.PushStatValue(perBodyStatDef.FindStatDef(bodyName ?? ""), statValue);
				}
			}
		}
	}

	private static void OnServerGameOver(Run run, GameEndingDef gameEndingDef)
	{
		if (!gameEndingDef.isWin || !(run.GetType() == typeof(Run)))
		{
			return;
		}
		foreach (PlayerStatsComponent instances in PlayerStatsComponent.instancesList)
		{
			if (instances.playerCharacterMasterController.isConnected)
			{
				instances.currentStats.PushStatValue(PerBodyStatDef.totalWins.FindStatDef(instances.characterMaster.bodyPrefab?.name ?? ""), 1uL);
			}
		}
	}

	private static void OnPlayerFirstCreatedServer(Run run, PlayerCharacterMasterController playerCharacterMasterController)
	{
		playerCharacterMasterController.master.onBodyStart += OnBodyFirstStart;
	}

	private static void OnBodyFirstStart(CharacterBody body)
	{
		CharacterMaster master = body.master;
		if (!master)
		{
			return;
		}
		master.onBodyStart -= OnBodyFirstStart;
		PlayerCharacterMasterController component = master.GetComponent<PlayerCharacterMasterController>();
		if ((bool)component)
		{
			PlayerStatsComponent component2 = component.GetComponent<PlayerStatsComponent>();
			if ((bool)component2)
			{
				StatSheet currentStats = component2.currentStats;
				currentStats.PushStatValue(PerBodyStatDef.timesPicked.FindStatDef(body.name), 1uL);
				currentStats.PushStatValue(StatDef.totalGamesPlayed, 1uL);
			}
		}
	}

	public static void ForceUpdate()
	{
		ProcessEvents();
	}

	private static void ProcessEvents()
	{
		ProcessDamageEvents();
		ProcessDeathEvents();
		ProcessHealingEvents();
		ProcessGoldEvents();
		ProcessItemCollectedEvents();
		ProcessCharacterUpdateEvents();
	}

	public static void OnCharacterHeal(HealthComponent healthComponent, float amount, ProcChainMask procChainMask)
	{
		healingEvents.Enqueue(new HealingEvent
		{
			healee = healthComponent.gameObject,
			healAmount = amount
		});
	}

	public static void OnDamageDealt(DamageReport damageReport)
	{
		damageEvents.Enqueue(new DamageEvent
		{
			attackerMaster = damageReport.attackerMaster,
			attackerBodyIndex = damageReport.attackerBodyIndex,
			attackerOwnerMaster = damageReport.attackerOwnerMaster,
			attackerOwnerBodyIndex = damageReport.attackerOwnerBodyIndex,
			victimMaster = damageReport.victimMaster,
			victimBodyIndex = damageReport.victimBodyIndex,
			victimIsElite = damageReport.victimIsElite,
			damageDealt = damageReport.damageDealt,
			dotType = damageReport.dotType
		});
	}

	public static void OnCharacterExecute(DamageReport damageReport, float executionHealthLost)
	{
		damageEvents.Enqueue(new DamageEvent
		{
			attackerMaster = damageReport.attackerMaster,
			attackerBodyIndex = damageReport.attackerBodyIndex,
			attackerOwnerMaster = damageReport.attackerOwnerMaster,
			attackerOwnerBodyIndex = damageReport.attackerOwnerBodyIndex,
			victimMaster = damageReport.victimMaster,
			victimBodyIndex = damageReport.victimBodyIndex,
			victimIsElite = damageReport.victimIsElite,
			damageDealt = executionHealthLost,
			dotType = damageReport.dotType
		});
	}

	public static void OnCharacterDeath(DamageReport damageReport)
	{
		DotController dotController = DotController.FindDotController(damageReport.victim.gameObject);
		bool victimWasBurning = false;
		if ((bool)dotController)
		{
			victimWasBurning = dotController.HasDotActive(DotController.DotIndex.Burn) | dotController.HasDotActive(DotController.DotIndex.PercentBurn) | dotController.HasDotActive(DotController.DotIndex.Helfire) | dotController.HasDotActive(DotController.DotIndex.StrongerBurn);
		}
		deathEvents.Enqueue(new DeathEvent
		{
			damageReport = damageReport,
			victimWasBurning = victimWasBurning
		});
	}

	private static void ProcessHealingEvents()
	{
		while (healingEvents.Count > 0)
		{
			HealingEvent healingEvent = healingEvents.Dequeue();
			ulong statValue = (ulong)healingEvent.healAmount;
			PlayerStatsComponent.FindBodyStatSheet(healingEvent.healee)?.PushStatValue(StatDef.totalHealthHealed, statValue);
		}
	}

	private static void ProcessDamageEvents()
	{
		while (damageEvents.Count > 0)
		{
			DamageEvent damageEvent = damageEvents.Dequeue();
			ulong statValue = (ulong)damageEvent.damageDealt;
			StatSheet statSheet = PlayerStatsComponent.FindMasterStatSheet(damageEvent.victimMaster);
			StatSheet statSheet2 = PlayerStatsComponent.FindMasterStatSheet(damageEvent.attackerMaster);
			StatSheet statSheet3 = PlayerStatsComponent.FindMasterStatSheet(damageEvent.attackerOwnerMaster);
			if (statSheet != null)
			{
				statSheet.PushStatValue(StatDef.totalDamageTaken, statValue);
				if (damageEvent.attackerBodyIndex != BodyIndex.None)
				{
					statSheet.PushStatValue(PerBodyStatDef.damageTakenFrom, damageEvent.attackerBodyIndex, statValue);
				}
				if (damageEvent.victimBodyIndex != BodyIndex.None)
				{
					statSheet.PushStatValue(PerBodyStatDef.damageTakenAs, damageEvent.victimBodyIndex, statValue);
				}
			}
			if (statSheet2 != null)
			{
				statSheet2.PushStatValue(StatDef.totalDamageDealt, statValue);
				statSheet2.PushStatValue(StatDef.highestDamageDealt, statValue);
				if (damageEvent.attackerBodyIndex != BodyIndex.None)
				{
					statSheet2.PushStatValue(PerBodyStatDef.damageDealtAs, damageEvent.attackerBodyIndex, statValue);
				}
				if (damageEvent.victimBodyIndex != BodyIndex.None)
				{
					statSheet2.PushStatValue(PerBodyStatDef.damageDealtTo, damageEvent.victimBodyIndex, statValue);
				}
			}
			if (statSheet3 != null)
			{
				statSheet3.PushStatValue(StatDef.totalMinionDamageDealt, statValue);
				if (damageEvent.attackerOwnerBodyIndex != BodyIndex.None)
				{
					statSheet3.PushStatValue(PerBodyStatDef.minionDamageDealtAs, damageEvent.attackerOwnerBodyIndex, statValue);
				}
			}
		}
	}

	private static void ProcessDeathEvents()
	{
		while (deathEvents.Count > 0)
		{
			DeathEvent deathEvent = deathEvents.Dequeue();
			DamageReport damageReport = deathEvent.damageReport;
			StatSheet statSheet = PlayerStatsComponent.FindMasterStatSheet(damageReport.victimMaster);
			StatSheet statSheet2 = PlayerStatsComponent.FindMasterStatSheet(damageReport.attackerMaster);
			StatSheet statSheet3 = PlayerStatsComponent.FindMasterStatSheet(damageReport.attackerOwnerMaster);
			if (statSheet != null)
			{
				statSheet.PushStatValue(StatDef.totalDeaths, 1uL);
				statSheet.PushStatValue(PerBodyStatDef.deathsAs, damageReport.victimBodyIndex, 1uL);
				if (damageReport.attackerBodyIndex != BodyIndex.None)
				{
					statSheet.PushStatValue(PerBodyStatDef.deathsFrom, damageReport.attackerBodyIndex, 1uL);
				}
				if (damageReport.dotType != DotController.DotIndex.None)
				{
					DotController.DotIndex dotType = damageReport.dotType;
					if ((uint)(dotType - 1) <= 2u || dotType == DotController.DotIndex.StrongerBurn)
					{
						statSheet.PushStatValue(StatDef.totalBurnDeaths, 1uL);
					}
				}
				if (deathEvent.victimWasBurning)
				{
					statSheet.PushStatValue(StatDef.totalDeathsWhileBurning, 1uL);
				}
			}
			if (statSheet2 != null)
			{
				statSheet2.PushStatValue(StatDef.totalKills, 1uL);
				statSheet2.PushStatValue(PerBodyStatDef.killsAs, damageReport.attackerBodyIndex, 1uL);
				if (damageReport.victimBodyIndex != BodyIndex.None)
				{
					statSheet2.PushStatValue(PerBodyStatDef.killsAgainst, damageReport.victimBodyIndex, 1uL);
					if (damageReport.victimIsElite)
					{
						statSheet2.PushStatValue(StatDef.totalEliteKills, 1uL);
						statSheet2.PushStatValue(PerBodyStatDef.killsAgainstElite, damageReport.victimBodyIndex, 1uL);
					}
				}
				if (damageReport.attackerBodyIndex == crocoBodyIndex && damageReport.combinedHealthBeforeDamage <= 1f)
				{
					statSheet2.PushStatValue(StatDef.totalCrocoWeakEnemyKills, 1uL);
				}
				string text = damageReport.victimBody?.customKillTotalStatName;
				if (!string.IsNullOrEmpty(text))
				{
					StatDef statDef = StatDef.Find(text);
					if (statDef == null)
					{
						Debug.LogWarningFormat("Stat def \"{0}\" could not be found.", text);
					}
					else
					{
						statSheet2.PushStatValue(statDef, 1uL);
					}
				}
			}
			if (statSheet3 != null)
			{
				statSheet3.PushStatValue(StatDef.totalMinionKills, 1uL);
				if (damageReport.attackerOwnerBodyIndex != BodyIndex.None)
				{
					statSheet3.PushStatValue(PerBodyStatDef.minionKillsAs, damageReport.attackerOwnerBodyIndex, 1uL);
				}
			}
			if (!damageReport.victimIsBoss)
			{
				continue;
			}
			int i = 0;
			for (int count = PlayerStatsComponent.instancesList.Count; i < count; i++)
			{
				PlayerStatsComponent playerStatsComponent = PlayerStatsComponent.instancesList[i];
				if (playerStatsComponent.characterMaster.hasBody)
				{
					playerStatsComponent.currentStats.PushStatValue(StatDef.totalTeleporterBossKillsWitnessed, 1uL);
				}
			}
		}
	}

	public static void OnGoldCollected(CharacterMaster characterMaster, ulong amount)
	{
		goldCollectedEvents.Enqueue(new GoldEvent
		{
			characterMaster = characterMaster,
			amount = amount
		});
	}

	private static void ProcessGoldEvents()
	{
		while (goldCollectedEvents.Count > 0)
		{
			GoldEvent goldEvent = goldCollectedEvents.Dequeue();
			StatSheet statSheet = goldEvent.characterMaster?.GetComponent<PlayerStatsComponent>()?.currentStats;
			if (statSheet != null)
			{
				statSheet.PushStatValue(StatDef.goldCollected, goldEvent.amount);
				statSheet.PushStatValue(StatDef.maxGoldCollected, statSheet.GetStatValueULong(StatDef.goldCollected));
			}
		}
	}

	public static void OnPurchase<T>(CharacterBody characterBody, CostTypeIndex costType, T statDefsToIncrement) where T : IEnumerable<StatDef>
	{
		StatSheet statSheet = PlayerStatsComponent.FindBodyStatSheet(characterBody);
		if (statSheet == null)
		{
			return;
		}
		StatDef statDef = null;
		StatDef statDef2 = null;
		switch (costType)
		{
		case CostTypeIndex.Money:
			statDef = StatDef.totalGoldPurchases;
			statDef2 = StatDef.highestGoldPurchases;
			break;
		case CostTypeIndex.PercentHealth:
			statDef = StatDef.totalBloodPurchases;
			statDef2 = StatDef.highestBloodPurchases;
			break;
		case CostTypeIndex.LunarCoin:
			statDef = StatDef.totalLunarPurchases;
			statDef2 = StatDef.highestLunarPurchases;
			break;
		case CostTypeIndex.WhiteItem:
			statDef = StatDef.totalTier1Purchases;
			statDef2 = StatDef.highestTier1Purchases;
			break;
		case CostTypeIndex.GreenItem:
			statDef = StatDef.totalTier2Purchases;
			statDef2 = StatDef.highestTier2Purchases;
			break;
		case CostTypeIndex.RedItem:
			statDef = StatDef.totalTier3Purchases;
			statDef2 = StatDef.highestTier3Purchases;
			break;
		}
		statSheet.PushStatValue(StatDef.totalPurchases, 1uL);
		statSheet.PushStatValue(StatDef.highestPurchases, statSheet.GetStatValueULong(StatDef.totalPurchases));
		if (statDef != null)
		{
			statSheet.PushStatValue(statDef, 1uL);
			if (statDef2 != null)
			{
				statSheet.PushStatValue(statDef2, statSheet.GetStatValueULong(statDef));
			}
		}
		if (statDefsToIncrement == null)
		{
			return;
		}
		foreach (StatDef item in statDefsToIncrement)
		{
			if (item != null)
			{
				statSheet.PushStatValue(item, 1uL);
			}
		}
	}

	public static void OnEquipmentActivated(EquipmentSlot activator, EquipmentIndex equipmentIndex)
	{
		PlayerStatsComponent.FindBodyStatSheet(activator.characterBody)?.PushStatValue(PerEquipmentStatDef.totalTimesFired.FindStatDef(equipmentIndex), 1uL);
	}

	public static void PushCharacterUpdateEvent(CharacterUpdateEvent e)
	{
		characterUpdateEvents.Enqueue(e);
	}

	private static void ProcessCharacterUpdateEvents()
	{
		while (characterUpdateEvents.Count > 0)
		{
			CharacterUpdateEvent characterUpdateEvent = characterUpdateEvents.Dequeue();
			if (!characterUpdateEvent.statsComponent)
			{
				continue;
			}
			StatSheet currentStats = characterUpdateEvent.statsComponent.currentStats;
			if (currentStats != null)
			{
				BodyIndex bodyIndex = characterUpdateEvent.statsComponent.characterMaster.GetBody()?.bodyIndex ?? BodyIndex.None;
				currentStats.PushStatValue(StatDef.totalTimeAlive, characterUpdateEvent.additionalTimeAlive);
				currentStats.PushStatValue(StatDef.highestLevel, (ulong)characterUpdateEvent.level);
				currentStats.PushStatValue(StatDef.totalDistanceTraveled, characterUpdateEvent.additionalDistanceTraveled);
				if (bodyIndex != BodyIndex.None)
				{
					currentStats.PushStatValue(PerBodyStatDef.totalTimeAlive, bodyIndex, characterUpdateEvent.additionalTimeAlive);
					currentStats.PushStatValue(PerBodyStatDef.longestRun, bodyIndex, characterUpdateEvent.runTime);
				}
				EquipmentIndex currentEquipmentIndex = characterUpdateEvent.statsComponent.characterMaster.inventory.currentEquipmentIndex;
				if (currentEquipmentIndex != EquipmentIndex.None)
				{
					currentStats.PushStatValue(PerEquipmentStatDef.totalTimeHeld.FindStatDef(currentEquipmentIndex), characterUpdateEvent.additionalTimeAlive);
				}
			}
		}
	}

	private static void OnServerItemGiven(Inventory inventory, ItemIndex itemIndex, int quantity)
	{
		itemCollectedEvents.Enqueue(new ItemCollectedEvent
		{
			inventory = inventory,
			itemIndex = itemIndex,
			quantity = quantity,
			newCount = inventory.GetItemCount(itemIndex)
		});
	}

	private static void ProcessItemCollectedEvents()
	{
		while (itemCollectedEvents.Count > 0)
		{
			ItemCollectedEvent itemCollectedEvent = itemCollectedEvents.Dequeue();
			if ((bool)itemCollectedEvent.inventory)
			{
				StatSheet statSheet = itemCollectedEvent.inventory.GetComponent<PlayerStatsComponent>()?.currentStats;
				if (statSheet != null)
				{
					statSheet.PushStatValue(StatDef.totalItemsCollected, (ulong)itemCollectedEvent.quantity);
					statSheet.PushStatValue(StatDef.highestItemsCollected, statSheet.GetStatValueULong(StatDef.totalItemsCollected));
					statSheet.PushStatValue(PerItemStatDef.totalCollected.FindStatDef(itemCollectedEvent.itemIndex), (ulong)itemCollectedEvent.quantity);
					statSheet.PushStatValue(PerItemStatDef.highestCollected.FindStatDef(itemCollectedEvent.itemIndex), (ulong)itemCollectedEvent.newCount);
				}
			}
		}
	}

	private static void OnServerStageBegin(Stage stage)
	{
		foreach (PlayerStatsComponent instances in PlayerStatsComponent.instancesList)
		{
			if (instances.playerCharacterMasterController.isConnected)
			{
				StatSheet currentStats = instances.currentStats;
				StatDef statDef = PerStageStatDef.totalTimesVisited.FindStatDef(stage.sceneDef ? stage.sceneDef.baseSceneName : string.Empty);
				if (statDef != null)
				{
					currentStats.PushStatValue(statDef, 1uL);
				}
			}
		}
	}

	private static void OnServerStageComplete(Stage stage)
	{
		foreach (PlayerStatsComponent instances in PlayerStatsComponent.instancesList)
		{
			if (instances.playerCharacterMasterController.isConnected)
			{
				StatSheet currentStats = instances.currentStats;
				if (SceneInfo.instance.countsAsStage)
				{
					currentStats.PushStatValue(StatDef.totalStagesCompleted, 1uL);
					currentStats.PushStatValue(StatDef.highestStagesCompleted, currentStats.GetStatValueULong(StatDef.totalStagesCompleted));
				}
				StatDef statDef = PerStageStatDef.totalTimesCleared.FindStatDef(stage.sceneDef ? stage.sceneDef.baseSceneName : string.Empty);
				if (statDef != null)
				{
					currentStats.PushStatValue(statDef, 1uL);
				}
			}
		}
	}
}

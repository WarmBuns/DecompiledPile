using System;
using System.Collections.Generic;
using System.Globalization;
using JetBrains.Annotations;

namespace RoR2.Stats;

public class StatDef
{
	public delegate string DisplayValueFormatterDelegate(ref StatField statField);

	public static readonly List<StatDef> allStatDefs;

	private static readonly Dictionary<string, StatDef> nameToStatDef;

	public int index;

	public readonly string name;

	public readonly string displayToken;

	public readonly StatRecordType recordType;

	public readonly StatDataType dataType;

	public double pointValue;

	public readonly DisplayValueFormatterDelegate displayValueFormatter;

	public Action onChangeCallback;

	public static readonly StatDef totalGamesPlayed;

	public static readonly StatDef totalTimeAlive;

	public static readonly StatDef totalKills;

	public static readonly StatDef totalMinionKills;

	public static readonly StatDef totalDeaths;

	public static readonly StatDef totalDamageDealt;

	public static readonly StatDef totalMinionDamageDealt;

	public static readonly StatDef totalDamageTaken;

	public static readonly StatDef totalHealthHealed;

	public static readonly StatDef highestDamageDealt;

	public static readonly StatDef highestLevel;

	public static readonly StatDef goldCollected;

	public static readonly StatDef maxGoldCollected;

	public static readonly StatDef totalDistanceTraveled;

	public static readonly StatDef totalItemsCollected;

	public static readonly StatDef highestItemsCollected;

	public static readonly StatDef totalStagesCompleted;

	public static readonly StatDef highestStagesCompleted;

	public static readonly StatDef highestInfiniteTowerWaveReached;

	public static readonly StatDef totalPurchases;

	public static readonly StatDef highestPurchases;

	public static readonly StatDef totalGoldPurchases;

	public static readonly StatDef highestGoldPurchases;

	public static readonly StatDef totalBloodPurchases;

	public static readonly StatDef highestBloodPurchases;

	public static readonly StatDef totalLunarPurchases;

	public static readonly StatDef highestLunarPurchases;

	public static readonly StatDef totalTier1Purchases;

	public static readonly StatDef highestTier1Purchases;

	public static readonly StatDef totalTier2Purchases;

	public static readonly StatDef highestTier2Purchases;

	public static readonly StatDef totalTier3Purchases;

	public static readonly StatDef highestTier3Purchases;

	public static readonly StatDef totalDronesPurchased;

	public static readonly StatDef totalTurretsPurchased;

	public static readonly StatDef totalGreenSoupsPurchased;

	public static readonly StatDef totalRedSoupsPurchased;

	public static readonly StatDef suicideHermitCrabsAchievementProgress;

	public static readonly StatDef firstTeleporterCompleted;

	public static readonly StatDef totalEliteKills;

	public static readonly StatDef totalBurnDeaths;

	public static readonly StatDef totalDeathsWhileBurning;

	public static readonly StatDef totalTeleporterBossKillsWitnessed;

	public static readonly StatDef totalCrocoInfectionsInflicted;

	public static readonly StatDef totalCrocoWeakEnemyKills;

	public static readonly StatDef totalMaulingRockKills;

	public static readonly StatDef chefAlternateSpecialBarbecueBisonAchievementProgress;

	private static string[] bodyNames;

	[CanBeNull]
	public static StatDef Find(string statName)
	{
		nameToStatDef.TryGetValue(statName, out var value);
		return value;
	}

	private StatDef(string name, StatRecordType recordType, StatDataType dataType, double pointValue, DisplayValueFormatterDelegate displayValueFormatter)
	{
		this.name = name;
		this.recordType = recordType;
		this.dataType = dataType;
		this.pointValue = pointValue;
		this.displayValueFormatter = displayValueFormatter;
		displayToken = "STATNAME_" + name.ToUpper(CultureInfo.InvariantCulture);
	}

	static StatDef()
	{
		allStatDefs = new List<StatDef>();
		nameToStatDef = new Dictionary<string, StatDef>();
		totalGamesPlayed = Register("totalGamesPlayed", StatRecordType.Sum, StatDataType.ULong, 0.0);
		totalTimeAlive = Register("totalTimeAlive", StatRecordType.Sum, StatDataType.Double, 1.0, TimeMMSSDisplayValueFormatter);
		totalKills = Register("totalKills", StatRecordType.Sum, StatDataType.ULong, 10.0);
		totalMinionKills = Register("totalMinionKills", StatRecordType.Sum, StatDataType.ULong, 10.0);
		totalDeaths = Register("totalDeaths", StatRecordType.Sum, StatDataType.ULong, 0.0);
		totalDamageDealt = Register("totalDamageDealt", StatRecordType.Sum, StatDataType.ULong, 0.01);
		totalMinionDamageDealt = Register("totalMinionDamageDealt", StatRecordType.Sum, StatDataType.ULong, 0.01);
		totalDamageTaken = Register("totalDamageTaken", StatRecordType.Sum, StatDataType.ULong, 0.0);
		totalHealthHealed = Register("totalHealthHealed", StatRecordType.Sum, StatDataType.ULong, 0.01);
		highestDamageDealt = Register("highestDamageDealt", StatRecordType.Max, StatDataType.ULong, 1.0);
		highestLevel = Register("highestLevel", StatRecordType.Max, StatDataType.ULong, 100.0);
		goldCollected = Register("totalGoldCollected", StatRecordType.Sum, StatDataType.ULong, 1.0);
		maxGoldCollected = Register("maxGoldCollected", StatRecordType.Max, StatDataType.ULong, 0.0);
		totalDistanceTraveled = Register("totalDistanceTraveled", StatRecordType.Sum, StatDataType.Double, 0.01, DistanceMarathonsDisplayValueFormatter);
		totalItemsCollected = Register("totalItemsCollected", StatRecordType.Sum, StatDataType.ULong, 110.0);
		highestItemsCollected = Register("highestItemsCollected", StatRecordType.Max, StatDataType.ULong, 10.0);
		totalStagesCompleted = Register("totalStagesCompleted", StatRecordType.Sum, StatDataType.ULong, 100.0);
		highestStagesCompleted = Register("highestStagesCompleted", StatRecordType.Max, StatDataType.ULong, 0.0);
		highestInfiniteTowerWaveReached = Register("highestInfiniteTowerWaveReached", StatRecordType.Max, StatDataType.ULong, 100.0);
		totalPurchases = Register("totalPurchases", StatRecordType.Sum, StatDataType.ULong, 35.0);
		highestPurchases = Register("highestPurchases", StatRecordType.Max, StatDataType.ULong, 0.0);
		totalGoldPurchases = Register("totalGoldPurchases", StatRecordType.Sum, StatDataType.ULong, 0.0);
		highestGoldPurchases = Register("highestGoldPurchases", StatRecordType.Max, StatDataType.ULong, 0.0);
		totalBloodPurchases = Register("totalBloodPurchases", StatRecordType.Sum, StatDataType.ULong, 0.0);
		highestBloodPurchases = Register("highestBloodPurchases", StatRecordType.Max, StatDataType.ULong, 0.0);
		totalLunarPurchases = Register("totalLunarPurchases", StatRecordType.Sum, StatDataType.ULong, 0.0);
		highestLunarPurchases = Register("highestLunarPurchases", StatRecordType.Max, StatDataType.ULong, 0.0);
		totalTier1Purchases = Register("totalTier1Purchases", StatRecordType.Sum, StatDataType.ULong, 0.0);
		highestTier1Purchases = Register("highestTier1Purchases", StatRecordType.Max, StatDataType.ULong, 0.0);
		totalTier2Purchases = Register("totalTier2Purchases", StatRecordType.Sum, StatDataType.ULong, 0.0);
		highestTier2Purchases = Register("highestTier2Purchases", StatRecordType.Max, StatDataType.ULong, 0.0);
		totalTier3Purchases = Register("totalTier3Purchases", StatRecordType.Sum, StatDataType.ULong, 0.0);
		highestTier3Purchases = Register("highestTier3Purchases", StatRecordType.Max, StatDataType.ULong, 0.0);
		totalDronesPurchased = Register("totalDronesPurchased", StatRecordType.Sum, StatDataType.ULong, 0.0);
		totalTurretsPurchased = Register("totalTurretsPurchased", StatRecordType.Sum, StatDataType.ULong, 0.0);
		totalGreenSoupsPurchased = Register("totalGreenSoupsPurchased", StatRecordType.Sum, StatDataType.ULong, 0.0);
		totalRedSoupsPurchased = Register("totalRedSoupsPurchased", StatRecordType.Sum, StatDataType.ULong, 0.0);
		suicideHermitCrabsAchievementProgress = Register("suicideHermitCrabsAchievementProgress", StatRecordType.Sum, StatDataType.ULong, 0.0);
		firstTeleporterCompleted = Register("firstTeleporterCompleted", StatRecordType.Sum, StatDataType.ULong, 0.0);
		totalEliteKills = Register("totalEliteKills", StatRecordType.Sum, StatDataType.ULong, 0.0);
		totalBurnDeaths = Register("totalBurnDeaths", StatRecordType.Sum, StatDataType.ULong, 0.0);
		totalDeathsWhileBurning = Register("totalDeathsWhileBurning", StatRecordType.Sum, StatDataType.ULong, 0.0);
		totalTeleporterBossKillsWitnessed = Register("totalTeleporterBossKillsWitnessed", StatRecordType.Sum, StatDataType.ULong, 0.0);
		totalCrocoInfectionsInflicted = Register("totalCrocoInfectionsInflicted", StatRecordType.Sum, StatDataType.ULong, 0.0);
		totalCrocoWeakEnemyKills = Register("totalCrocoWeakEnemyKills", StatRecordType.Sum, StatDataType.ULong, 0.0);
		totalMaulingRockKills = Register("totalMaulingRockKills", StatRecordType.Sum, StatDataType.ULong, 0.0);
		chefAlternateSpecialBarbecueBisonAchievementProgress = Register("chefAlternateSpecialBarbecueBisonAchievementProgress", StatRecordType.Sum, StatDataType.ULong, 0.0);
	}

	[SystemInitializer(new Type[]
	{
		typeof(BodyCatalog),
		typeof(ItemCatalog),
		typeof(EquipmentCatalog),
		typeof(SceneCatalog)
	})]
	private static void Init()
	{
		PerBodyStatDef.RegisterStatDefs();
		PerItemStatDef.RegisterStatDefs();
		PerEquipmentStatDef.RegisterStatDefs();
		PerStageStatDef.RegisterStatDefs();
	}

	public static StatDef Register(string name, StatRecordType recordType, StatDataType dataType, double pointValue, DisplayValueFormatterDelegate displayValueFormatter = null)
	{
		if (displayValueFormatter == null)
		{
			displayValueFormatter = DefaultDisplayValueFormatter;
		}
		StatDef statDef = new StatDef(name, recordType, dataType, pointValue, displayValueFormatter)
		{
			index = allStatDefs.Count
		};
		allStatDefs.Add(statDef);
		nameToStatDef.Add(statDef.name, statDef);
		return statDef;
	}

	public static string DefaultDisplayValueFormatter(ref StatField statField)
	{
		return statField.ToLocalNumeric();
	}

	public static string TimeMMSSDisplayValueFormatter(ref StatField statField)
	{
		ulong num = 0uL;
		num = statField.dataType switch
		{
			StatDataType.ULong => statField.GetULongValue(), 
			StatDataType.Double => (ulong)statField.GetDoubleValue(), 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		ulong num2 = num / 60;
		ulong num3 = num - num2 * 60;
		return $"{num2:00}:{num3:00}";
	}

	public static string DistanceMarathonsDisplayValueFormatter(ref StatField statField)
	{
		double num = 0.0;
		return string.Format(arg0: statField.dataType switch
		{
			StatDataType.ULong => statField.GetULongValue(), 
			StatDataType.Double => statField.GetDoubleValue(), 
			_ => throw new ArgumentOutOfRangeException(), 
		} * 2.3699E-05, format: Language.GetString("STAT_VALUE_MARATHONS_FORMAT"));
	}
}

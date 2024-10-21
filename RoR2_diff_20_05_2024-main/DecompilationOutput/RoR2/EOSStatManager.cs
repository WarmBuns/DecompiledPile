using Epic.OnlineServices;
using Epic.OnlineServices.Stats;

namespace RoR2;

public class EOSStatManager
{
	public static class StatNames
	{
		public const string kFastestWeeklyRun = "FASTESTWEEKLYRUN";
	}

	private static StatsInterface Interface;

	public EOSStatManager()
	{
		Interface = EOSPlatformManager.GetPlatformInterface().GetStatsInterface();
	}

	public static void IngestStat(IngestData[] statsToIngest, object callbackData = null, OnIngestStatCompleteCallback callback = null)
	{
		IngestStatOptions ingestStatOptions = default(IngestStatOptions);
		ingestStatOptions.LocalUserId = EOSLoginManager.loggedInProductId;
		ingestStatOptions.Stats = statsToIngest;
		ingestStatOptions.TargetUserId = EOSLoginManager.loggedInProductId;
		IngestStatOptions options = ingestStatOptions;
		Interface.IngestStat(ref options, callbackData, callback);
	}

	public static void QueryStats(ref QueryStatsOptions queryStatsOptions, object callbackData = null, OnQueryStatsCompleteCallback callback = null)
	{
		Interface.QueryStats(ref queryStatsOptions, callbackData, callback);
	}

	public static Stat GetStat(string statName, ProductUserId targetUserId)
	{
		CopyStatByNameOptions copyStatByNameOptions = default(CopyStatByNameOptions);
		copyStatByNameOptions.Name = statName;
		copyStatByNameOptions.TargetUserId = targetUserId;
		CopyStatByNameOptions options = copyStatByNameOptions;
		Interface.CopyStatByName(ref options, out var outStat);
		return outStat.Value;
	}

	public static uint GetStatsCount(ProductUserId targetUserId)
	{
		GetStatCountOptions getStatCountOptions = default(GetStatCountOptions);
		getStatCountOptions.TargetUserId = targetUserId;
		GetStatCountOptions options = getStatCountOptions;
		return Interface.GetStatsCount(ref options);
	}
}

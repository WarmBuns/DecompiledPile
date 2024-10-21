using System;
using System.Collections.Generic;
using System.Linq;
using Epic.OnlineServices;
using Epic.OnlineServices.Leaderboards;

namespace RoR2.UI;

public class EOSLeaderboardManager : LeaderboardManager
{
	public const string kPrismaticTrialsLeaderboardId = "PrismaticTrials";

	private readonly LeaderboardsInterface _leaderboardsInterface;

	private readonly List<LeaderboardRecord> _currentGlobalLeaderboardList = new List<LeaderboardRecord>();

	private readonly List<LeaderboardUserScore> _currentFriendsLeaderboardList = new List<LeaderboardUserScore>();

	private static bool _isGlobal;

	private static ProductUserId LocalUserId => EOSLoginManager.loggedInProductId;

	private static DateTime StartTime => WeeklyRun.GetSeedCycleStartDateTime();

	private static DateTime EndTime => WeeklyRun.GetSeedCycleEndDateTime();

	public EOSLeaderboardManager(LeaderboardController leaderboardController)
		: base(leaderboardController)
	{
		_leaderboardsInterface = EOSPlatformManager.GetPlatformInterface().GetLeaderboardsInterface();
		QueryLeaderboardDefinitionsOptions options = new QueryLeaderboardDefinitionsOptions
		{
			LocalUserId = LocalUserId
		};
		_leaderboardsInterface.QueryLeaderboardDefinitions(ref options, null, LeaderboardQueryComplete);
		base.IsQuerying = true;
	}

	private void LeaderboardQueryComplete(ref OnQueryLeaderboardDefinitionsCompleteCallbackInfo data)
	{
		base.IsValid = data.ResultCode == Result.Success;
		base.IsQuerying = false;
	}

	internal override void UpdateLeaderboard()
	{
		switch (LeaderboardController.currentRequestType)
		{
		case RequestType.Global:
		case RequestType.GlobalAroundUser:
			QueryLeaderboardRanks();
			_isGlobal = true;
			break;
		case RequestType.Friends:
			QueryFriendsLeaderboard();
			_isGlobal = false;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		base.IsQuerying = true;
	}

	private void QueryFriendsLeaderboard()
	{
		ProductUserId[] userIds = EOSFriendsManager.FriendsProductUserIds.ToArray();
		QueryLeaderboardUserScoresOptions queryLeaderboardUserScoresOptions = default(QueryLeaderboardUserScoresOptions);
		queryLeaderboardUserScoresOptions.StartTime = StartTime;
		queryLeaderboardUserScoresOptions.EndTime = EndTime;
		queryLeaderboardUserScoresOptions.LocalUserId = LocalUserId;
		queryLeaderboardUserScoresOptions.StatInfo = new UserScoresQueryStatInfo[1]
		{
			new UserScoresQueryStatInfo
			{
				Aggregation = LeaderboardAggregation.Min,
				StatName = "FASTESTWEEKLYRUN"
			}
		};
		queryLeaderboardUserScoresOptions.UserIds = userIds;
		QueryLeaderboardUserScoresOptions options = queryLeaderboardUserScoresOptions;
		_leaderboardsInterface.QueryLeaderboardUserScores(ref options, "FASTESTWEEKLYRUN", FriendsQueryComplete);
	}

	private void FriendsQueryComplete(ref OnQueryLeaderboardUserScoresCompleteCallbackInfo data)
	{
		string text = data.ClientData?.ToString() ?? "";
		GetLeaderboardUserScoreCountOptions getLeaderboardUserScoreCountOptions = default(GetLeaderboardUserScoreCountOptions);
		getLeaderboardUserScoreCountOptions.StatName = text;
		GetLeaderboardUserScoreCountOptions options = getLeaderboardUserScoreCountOptions;
		uint leaderboardUserScoreCount = _leaderboardsInterface.GetLeaderboardUserScoreCount(ref options);
		UpdateFriendsLeaderboardList(text, leaderboardUserScoreCount);
	}

	private void UpdateFriendsLeaderboardList(string leaderboardName, uint leaderboardMax)
	{
		int num = LeaderboardController.currentPage * LeaderboardController.entriesPerPage;
		long num2 = ((num + LeaderboardController.entriesPerPage > leaderboardMax) ? (leaderboardMax - num) : LeaderboardController.entriesPerPage);
		_currentFriendsLeaderboardList.Clear();
		for (int i = num; i < num2; i++)
		{
			GetFriendsLeaderboardEntry(Convert.ToUInt32(i), leaderboardName);
		}
	}

	private void GetFriendsLeaderboardEntry(uint leaderboardRecordIndex, string statName)
	{
		CopyLeaderboardUserScoreByIndexOptions copyLeaderboardUserScoreByIndexOptions = default(CopyLeaderboardUserScoreByIndexOptions);
		copyLeaderboardUserScoreByIndexOptions.LeaderboardUserScoreIndex = leaderboardRecordIndex;
		copyLeaderboardUserScoreByIndexOptions.StatName = statName;
		CopyLeaderboardUserScoreByIndexOptions options = copyLeaderboardUserScoreByIndexOptions;
		if (_leaderboardsInterface.CopyLeaderboardUserScoreByIndex(ref options, out var outLeaderboardUserScore) == Result.Success && outLeaderboardUserScore.HasValue)
		{
			LeaderboardUserScore value = outLeaderboardUserScore.Value;
			(PlatformSystems.userManager as UserManagerEOS)?.QueryForDisplayNames(value.UserId, delegate
			{
				base.IsQuerying = false;
			});
			_currentFriendsLeaderboardList.Add(value);
		}
	}

	private void QueryLeaderboardRanks()
	{
		QueryLeaderboardRanksOptions queryLeaderboardRanksOptions = default(QueryLeaderboardRanksOptions);
		queryLeaderboardRanksOptions.LeaderboardId = "PrismaticTrials";
		queryLeaderboardRanksOptions.LocalUserId = LocalUserId;
		QueryLeaderboardRanksOptions options = queryLeaderboardRanksOptions;
		_leaderboardsInterface.QueryLeaderboardRanks(ref options, LeaderboardController.currentLeaderboardName, GlobalQueryComplete);
	}

	private void GlobalQueryComplete(ref OnQueryLeaderboardRanksCompleteCallbackInfo data)
	{
		string leaderboardName = data.ClientData?.ToString() ?? "";
		GetLeaderboardRecordCountOptions options = default(GetLeaderboardRecordCountOptions);
		uint leaderboardRecordCount = _leaderboardsInterface.GetLeaderboardRecordCount(ref options);
		UpdateGlobalLeaderboardList(leaderboardName, leaderboardRecordCount);
	}

	private void UpdateGlobalLeaderboardList(string leaderboardName, uint leaderboardMax)
	{
		int num = LeaderboardController.currentPage * LeaderboardController.entriesPerPage;
		long num2 = ((num + LeaderboardController.entriesPerPage > leaderboardMax) ? (leaderboardMax - num) : LeaderboardController.entriesPerPage);
		_currentGlobalLeaderboardList.Clear();
		for (int i = num; i < num2; i++)
		{
			GetGlobalLeaderboardEntry(Convert.ToUInt32(i), leaderboardName);
		}
	}

	private void GetGlobalLeaderboardEntry(uint leaderboardRecordIndex, string leaderboardName)
	{
		CopyLeaderboardRecordByIndexOptions copyLeaderboardRecordByIndexOptions = default(CopyLeaderboardRecordByIndexOptions);
		copyLeaderboardRecordByIndexOptions.LeaderboardRecordIndex = leaderboardRecordIndex;
		CopyLeaderboardRecordByIndexOptions options = copyLeaderboardRecordByIndexOptions;
		if (_leaderboardsInterface.CopyLeaderboardRecordByIndex(ref options, out var outLeaderboardRecord) == Result.Success && outLeaderboardRecord.HasValue)
		{
			LeaderboardRecord value = outLeaderboardRecord.Value;
			(PlatformSystems.userManager as UserManagerEOS)?.QueryForDisplayNames(value.UserId, delegate
			{
				base.IsQuerying = false;
			});
			_currentGlobalLeaderboardList.Add(value);
		}
	}

	internal override List<LeaderboardInfo> GetLeaderboardInfoList()
	{
		if (_isGlobal)
		{
			return _currentGlobalLeaderboardList.Select(delegate(LeaderboardRecord leaderboardRecord)
			{
				LeaderboardInfo result = default(LeaderboardInfo);
				result.rank = Convert.ToInt32(leaderboardRecord.Rank);
				result.survivorIndex = null;
				result.timeInSeconds = leaderboardRecord.Score;
				result.userID = leaderboardRecord.UserId.ToString();
				return result;
			}).ToList();
		}
		_currentFriendsLeaderboardList.Sort((LeaderboardUserScore x, LeaderboardUserScore y) => x.Score.CompareTo(y.Score));
		return _currentFriendsLeaderboardList.Select(delegate(LeaderboardUserScore leaderboardUserScore, int index)
		{
			LeaderboardInfo result2 = default(LeaderboardInfo);
			result2.rank = index + 1;
			result2.survivorIndex = null;
			result2.timeInSeconds = leaderboardUserScore.Score;
			result2.userID = leaderboardUserScore.UserId.ToString();
			return result2;
		}).ToList();
	}

	internal override PlatformID GetUserID(LeaderboardInfo leaderboardInfo)
	{
		return new PlatformID(ProductUserId.FromString(leaderboardInfo.userID));
	}

	internal override string GetLocalUserIdString()
	{
		return LocalUserId.ToString();
	}
}

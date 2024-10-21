using System;
using System.Collections.Generic;
using System.Linq;
using Facepunch.Steamworks;

namespace RoR2.UI;

public class SteamLeaderboardManager : LeaderboardManager
{
	private Leaderboard _currentLeaderboard;

	private List<LeaderboardInfo> _leaderboardInfoList = new List<LeaderboardInfo>();

	public SteamLeaderboardManager(LeaderboardController leaderboardController)
		: base(leaderboardController)
	{
		base.IsValid = true;
	}

	internal override List<LeaderboardInfo> GetLeaderboardInfoList()
	{
		return _leaderboardInfoList;
	}

	internal override PlatformID GetUserID(LeaderboardInfo leaderboardInfo)
	{
		return new PlatformID(Convert.ToUInt64(leaderboardInfo.userID));
	}

	internal override string GetLocalUserIdString()
	{
		return Client.Instance.SteamId.ToString();
	}

	internal override void UpdateLeaderboard()
	{
		_currentLeaderboard = Client.Instance.GetLeaderboard(LeaderboardController.currentLeaderboardName);
		int num = LeaderboardController.currentPage * LeaderboardController.entriesPerPage - LeaderboardController.entriesPerPage / 2;
		base.IsValid = _currentLeaderboard != null && _currentLeaderboard.IsValid;
		base.IsQuerying = true;
		_currentLeaderboard.FetchScores((Leaderboard.RequestType)LeaderboardController.currentRequestType, num, num + LeaderboardController.entriesPerPage, delegate(Leaderboard.Entry[] entries)
		{
			_leaderboardInfoList.SetLeaderboardInfo(entries.Select(LeaderboardInfoFromSteamLeaderboardEntry).ToArray());
			base.IsQuerying = false;
			base.IsValid = _currentLeaderboard.IsValid;
		}, delegate
		{
		});
	}

	private static LeaderboardInfo LeaderboardInfoFromSteamLeaderboardEntry(Leaderboard.Entry entry)
	{
		SurvivorIndex value = SurvivorIndex.None;
		int num = ((entry.SubScores != null && entry.SubScores.Length >= 1) ? entry.SubScores[1] : 0);
		if (num >= 0 && num < SurvivorCatalog.survivorCount)
		{
			value = (SurvivorIndex)num;
		}
		LeaderboardInfo result = default(LeaderboardInfo);
		result.timeInSeconds = (float)entry.Score * 0.001f;
		result.survivorIndex = value;
		result.userID = entry.SteamId.ToString();
		result.rank = entry.GlobalRank;
		return result;
	}
}

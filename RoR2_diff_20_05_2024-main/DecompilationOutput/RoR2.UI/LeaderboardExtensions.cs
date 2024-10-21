using System.Collections.Generic;

namespace RoR2.UI;

internal static class LeaderboardExtensions
{
	internal static void SetLeaderboardInfo(this List<LeaderboardInfo> leaderboardInfoList, LeaderboardInfo[] leaderboardInfos)
	{
		leaderboardInfoList.Clear();
		leaderboardInfoList.AddRange(leaderboardInfos);
	}
}

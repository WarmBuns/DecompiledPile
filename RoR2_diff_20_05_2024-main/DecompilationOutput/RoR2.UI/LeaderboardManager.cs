using System.Collections.Generic;

namespace RoR2.UI;

public abstract class LeaderboardManager
{
	protected LeaderboardController LeaderboardController;

	public bool IsValid { get; protected set; }

	public bool IsQuerying { get; protected set; }

	internal abstract void UpdateLeaderboard();

	internal abstract List<LeaderboardInfo> GetLeaderboardInfoList();

	internal abstract PlatformID GetUserID(LeaderboardInfo leaderboardInfo);

	internal abstract string GetLocalUserIdString();

	protected LeaderboardManager(LeaderboardController leaderboardController)
	{
		LeaderboardController = leaderboardController;
	}
}

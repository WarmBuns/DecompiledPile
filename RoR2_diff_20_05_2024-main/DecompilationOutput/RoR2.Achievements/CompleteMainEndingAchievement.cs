using Assets.RoR2.Scripts.Platform;

namespace RoR2.Achievements;

[RegisterAchievement("CompleteMainEnding", "Characters.Captain", null, 3u, null)]
public class CompleteMainEndingAchievement : BaseEndingAchievement
{
	protected override bool ShouldGrant(RunReport runReport)
	{
		if (runReport.gameEnding == RoR2Content.GameEndings.MainEnding)
		{
			return true;
		}
		return false;
	}

	public override void TryToCompleteActivity()
	{
		bool flag = base.localUser.id == LocalUserManager.GetFirstLocalUser().id;
		if (shouldGrant && flag)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "CompleteMainEnding";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}
}

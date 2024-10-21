using Assets.RoR2.Scripts.Platform;

namespace RoR2.Achievements;

[RegisterAchievement("CompletePrismaticTrial", "Items.HealOnCrit", null, 2u, null)]
public class CompletePrismaticTrialAchievement : BaseEndingAchievement
{
	protected override bool ShouldGrant(RunReport runReport)
	{
		return runReport.gameEnding == GameEndingCatalog.FindGameEndingDef("PrismaticTrialEnding");
	}

	public override void TryToCompleteActivity()
	{
		bool flag = base.localUser.id == LocalUserManager.GetFirstLocalUser().id;
		if (shouldGrant && flag)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "CompletePrismaticTrial";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}
}

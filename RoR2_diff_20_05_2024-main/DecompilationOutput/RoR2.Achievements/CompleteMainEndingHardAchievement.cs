namespace RoR2.Achievements;

[RegisterAchievement("CompleteMainEndingHard", "Items.LunarBadLuck", null, 10u, null)]
public class CompleteMainEndingHardAchievement : BaseEndingAchievement
{
	protected override bool ShouldGrant(RunReport runReport)
	{
		if (runReport.gameEnding == RoR2Content.GameEndings.MainEnding && runReport.ruleBook.FindDifficulty() >= DifficultyIndex.Hard)
		{
			return true;
		}
		return false;
	}
}

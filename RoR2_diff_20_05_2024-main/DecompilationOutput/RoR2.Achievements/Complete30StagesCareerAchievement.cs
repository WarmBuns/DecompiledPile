using RoR2.Stats;

namespace RoR2.Achievements;

[RegisterAchievement("Complete30StagesCareer", "Characters.Engineer", null, 3u, null)]
public class Complete30StagesCareerAchievement : BaseStatMilestoneAchievement
{
	protected override StatDef statDef => StatDef.totalStagesCompleted;

	protected override ulong statRequirement => 30uL;
}

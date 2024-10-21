using RoR2.Stats;

namespace RoR2.Achievements;

[RegisterAchievement("Die5Times", "Items.Bear", null, 1u, null)]
public class Die5TimesAchievement : BaseStatMilestoneAchievement
{
	protected override StatDef statDef => StatDef.totalDeaths;

	protected override ulong statRequirement => 5uL;
}

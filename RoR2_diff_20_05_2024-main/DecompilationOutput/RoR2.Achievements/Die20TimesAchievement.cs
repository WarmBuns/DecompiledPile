using RoR2.Stats;

namespace RoR2.Achievements;

[RegisterAchievement("Die20Times", "Items.DeathProjectile", null, 2u, null)]
public class Die20TimesAchievement : BaseStatMilestoneAchievement
{
	protected override StatDef statDef => StatDef.totalDeaths;

	protected override ulong statRequirement => 20uL;
}

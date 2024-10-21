using RoR2.Stats;

namespace RoR2.Achievements;

[RegisterAchievement("KillTotalEnemies", "Items.Infusion", null, 2u, null)]
public class KillTotalEnemiesAchievement : BaseStatMilestoneAchievement
{
	protected override StatDef statDef => StatDef.totalKills;

	protected override ulong statRequirement => 3000uL;
}

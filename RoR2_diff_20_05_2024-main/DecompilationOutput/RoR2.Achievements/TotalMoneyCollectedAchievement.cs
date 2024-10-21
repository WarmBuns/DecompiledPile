using RoR2.Stats;

namespace RoR2.Achievements;

[RegisterAchievement("TotalMoneyCollected", "Items.GoldGat", null, 2u, null)]
public class TotalMoneyCollectedAchievement : BaseStatMilestoneAchievement
{
	protected override StatDef statDef => StatDef.goldCollected;

	protected override ulong statRequirement => 30480uL;
}

using RoR2.Stats;

namespace RoR2.Achievements;

[RegisterAchievement("BurnToDeath", "Items.Cleanse", null, 2u, null)]
public class BurnToDeathAchievement : BaseStatMilestoneAchievement
{
	protected override StatDef statDef => StatDef.totalDeathsWhileBurning;

	protected override ulong statRequirement => 3uL;
}

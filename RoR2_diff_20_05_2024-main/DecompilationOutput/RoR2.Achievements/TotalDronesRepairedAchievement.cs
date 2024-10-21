using RoR2.Stats;

namespace RoR2.Achievements;

[RegisterAchievement("TotalDronesRepaired", "Items.DroneBackup", null, 3u, null)]
public class TotalDronesRepairedAchievement : BaseStatMilestoneAchievement
{
	protected override StatDef statDef => StatDef.totalDronesPurchased;

	protected override ulong statRequirement => 30uL;
}

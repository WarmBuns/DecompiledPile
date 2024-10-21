using Assets.RoR2.Scripts.Platform;
using RoR2.Stats;

namespace RoR2.Achievements.Croco;

[RegisterAchievement("CrocoKillWeakEnemiesMilestone", "Skills.Croco.PassivePoisonLethal", "BeatArena", 3u, null)]
public class CrocoKillWeakEnemiesMilestoneAchievement : BaseStatMilestoneAchievement
{
	protected override StatDef statDef => StatDef.totalCrocoWeakEnemyKills;

	protected override ulong statRequirement => 50uL;

	public override void TryToCompleteActivity()
	{
		if (base.localUser.id == LocalUserManager.GetFirstLocalUser().id && shouldGrant)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "CrocoKillWeakEnemiesMilestone";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}
}

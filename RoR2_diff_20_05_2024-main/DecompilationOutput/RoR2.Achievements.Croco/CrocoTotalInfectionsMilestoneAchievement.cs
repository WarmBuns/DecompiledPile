using Assets.RoR2.Scripts.Platform;
using RoR2.Stats;

namespace RoR2.Achievements.Croco;

[RegisterAchievement("CrocoTotalInfectionsMilestone", "Skills.Croco.ChainableLeap", "BeatArena", 3u, null)]
public class CrocoTotalInfectionsMilestoneAchievement : BaseStatMilestoneAchievement
{
	protected override StatDef statDef => StatDef.totalCrocoInfectionsInflicted;

	protected override ulong statRequirement => 1000uL;

	public override void TryToCompleteActivity()
	{
		if (base.localUser.id == LocalUserManager.GetFirstLocalUser().id && shouldGrant)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "CrocoTotalInfectionsMilestone";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}
}

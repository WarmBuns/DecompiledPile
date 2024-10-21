using System;
using RoR2.Stats;

namespace RoR2.Achievements;

[RegisterAchievement("UseThreePortals", "Items.Tonic", null, 2u, null)]
public class UseThreePortalsAchievement : BaseAchievement
{
	private StatDef[] statsToCheck;

	private const int requirement = 3;

	public override void OnInstall()
	{
		base.OnInstall();
		statsToCheck = new StatDef[5]
		{
			PerStageStatDef.totalTimesVisited.FindStatDef("bazaar"),
			PerStageStatDef.totalTimesVisited.FindStatDef("mysteryspace"),
			PerStageStatDef.totalTimesVisited.FindStatDef("goldshores"),
			PerStageStatDef.totalTimesVisited.FindStatDef("arena"),
			PerStageStatDef.totalTimesVisited.FindStatDef("artifactworld")
		};
		UserProfile obj = base.userProfile;
		obj.onStatsReceived = (Action)Delegate.Combine(obj.onStatsReceived, new Action(Check));
		Check();
	}

	public override void OnUninstall()
	{
		UserProfile obj = base.userProfile;
		obj.onStatsReceived = (Action)Delegate.Remove(obj.onStatsReceived, new Action(Check));
		base.OnUninstall();
	}

	public override float ProgressForAchievement()
	{
		return (float)GetUniquePortalsUsedCount() / 3f;
	}

	private int GetUniquePortalsUsedCount()
	{
		StatSheet statSheet = base.userProfile.statSheet;
		int num = 0;
		StatDef[] array = statsToCheck;
		foreach (StatDef statDef in array)
		{
			if (statSheet.GetStatValueULong(statDef) != 0)
			{
				num++;
			}
		}
		return num;
	}

	private void Check()
	{
		if (GetUniquePortalsUsedCount() >= 3)
		{
			Grant();
		}
	}
}

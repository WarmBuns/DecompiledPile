using RoR2.Stats;

namespace RoR2.Achievements;

[RegisterAchievement("LogCollector", "Items.Scanner", null, 2u, null)]
public class LogCollectorAchievement : BaseAchievement
{
	private const int requirement = 10;

	public override void OnInstall()
	{
		base.OnInstall();
		Check();
		UserProfile.onUnlockableGranted += OnUnlockCheck;
	}

	public override void OnUninstall()
	{
		UserProfile.onUnlockableGranted -= OnUnlockCheck;
		base.OnUninstall();
	}

	public override float ProgressForAchievement()
	{
		return (float)MonsterLogCount() / 10f;
	}

	private static bool IsUnlockableMonsterLog(UnlockableDef unlockableDef)
	{
		return unlockableDef.cachedName.StartsWith("Logs.");
	}

	private int MonsterLogCount()
	{
		StatSheet statSheet = base.userProfile.statSheet;
		int num = 0;
		int i = 0;
		for (int unlockableCount = statSheet.GetUnlockableCount(); i < unlockableCount; i++)
		{
			if (IsUnlockableMonsterLog(statSheet.GetUnlockable(i)))
			{
				num++;
			}
		}
		return num;
	}

	private void Check()
	{
		if (MonsterLogCount() >= 10)
		{
			Grant();
		}
	}

	private void OnUnlockCheck(UserProfile userProfile, UnlockableDef unlockableDef)
	{
		if (userProfile == base.userProfile && IsUnlockableMonsterLog(unlockableDef))
		{
			Check();
		}
	}
}

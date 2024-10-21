using RoR2.Stats;

namespace RoR2.Achievements;

[RegisterAchievement("FindUniqueNewtStatues", "Items.Talisman", null, 3u, null)]
public class FindUniqueNewtStatues : BaseAchievement
{
	private const int requirement = 8;

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
		return (float)UniqueNewtStatueCount() / 8f;
	}

	private static bool IsUnlockableNewtStatue(UnlockableDef unlockableDef)
	{
		return unlockableDef.cachedName.StartsWith("NewtStatue.");
	}

	private int UniqueNewtStatueCount()
	{
		StatSheet statSheet = base.userProfile.statSheet;
		int num = 0;
		int i = 0;
		for (int unlockableCount = statSheet.GetUnlockableCount(); i < unlockableCount; i++)
		{
			if (IsUnlockableNewtStatue(statSheet.GetUnlockable(i)))
			{
				num++;
			}
		}
		return num;
	}

	private void Check()
	{
		if (UniqueNewtStatueCount() >= 8)
		{
			Grant();
		}
	}

	private void OnUnlockCheck(UserProfile userProfile, UnlockableDef unlockableDef)
	{
		if (userProfile == base.userProfile && IsUnlockableNewtStatue(unlockableDef))
		{
			Check();
		}
	}
}

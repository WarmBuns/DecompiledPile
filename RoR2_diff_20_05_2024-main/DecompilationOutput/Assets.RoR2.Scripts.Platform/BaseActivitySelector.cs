namespace Assets.RoR2.Scripts.Platform;

public class BaseActivitySelector
{
	public BaseActivity.ActivityGameType activityGameType = BaseActivity.ActivityGameType.Any;

	public BaseActivity.ActivityGameMode activityGameMode = BaseActivity.ActivityGameMode.Any;

	public BaseActivity.ActivitySurvivor activitySurvivor = BaseActivity.ActivitySurvivor.Any;

	public string activityAchievementID = "";

	public bool IsCompatibleWith(BaseActivity activity)
	{
		if ((activity.GameType == BaseActivity.ActivityGameType.Any || activityGameType == BaseActivity.ActivityGameType.Any || activity.GameType == activityGameType) && (activity.GameMode == BaseActivity.ActivityGameMode.Any || activityGameMode == BaseActivity.ActivityGameMode.Any || activity.GameMode == activityGameMode) && (activity.Survivor == BaseActivity.ActivitySurvivor.Any || activitySurvivor == BaseActivity.ActivitySurvivor.Any || activity.Survivor == activitySurvivor))
		{
			if (activity.AchievementID.Length != 0 && activityAchievementID.Length != 0)
			{
				return activity.AchievementID.CompareTo(activityAchievementID) == 0;
			}
			return true;
		}
		return false;
	}
}

namespace RoR2;

public abstract class AchievementSystem
{
	internal const int ACHIEVEMENT_COUNT = 38;

	public abstract void AddAchievement(string achievementName);

	public virtual void UpdateAchievementProgress(string achievementName, ulong statProgress, ulong statRequirement, ulong id)
	{
	}
}

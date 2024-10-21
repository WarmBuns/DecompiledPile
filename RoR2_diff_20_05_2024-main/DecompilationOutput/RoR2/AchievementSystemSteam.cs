using Facepunch.Steamworks;

namespace RoR2;

public class AchievementSystemSteam : AchievementSystem
{
	public override void AddAchievement(string achievementName)
	{
		Client.Instance.Achievements.Trigger(achievementName);
	}
}

using UnityEngine;

namespace RoR2;

public struct AchievementIndex
{
	[SerializeField]
	public int intValue;

	public static AchievementIndex operator ++(AchievementIndex achievementIndex)
	{
		achievementIndex.intValue++;
		return achievementIndex;
	}
}

using System;
using JetBrains.Annotations;

namespace RoR2;

[MeansImplicitUse]
public class RegisterAchievementAttribute : Attribute
{
	public readonly string identifier;

	public readonly string unlockableRewardIdentifier;

	public readonly string prerequisiteAchievementIdentifier;

	public readonly uint lunarCoinReward;

	public readonly Type serverTrackerType;

	public RegisterAchievementAttribute([NotNull] string identifier, string unlockableRewardIdentifier, string prerequisiteAchievementIdentifier, uint lunarCoinReward, Type serverTrackerType = null)
	{
		this.identifier = identifier;
		this.unlockableRewardIdentifier = unlockableRewardIdentifier;
		this.prerequisiteAchievementIdentifier = prerequisiteAchievementIdentifier;
		this.lunarCoinReward = lunarCoinReward;
		this.serverTrackerType = serverTrackerType;
	}
}

using Epic.OnlineServices;
using Epic.OnlineServices.Achievements;
using UnityEngine;

namespace RoR2;

public class AchievementSystemEOS : AchievementSystem
{
	private AchievementsInterface _achievementsInterface;

	public AchievementSystemEOS()
	{
		_achievementsInterface = EOSPlatformManager.GetPlatformInterface().GetAchievementsInterface();
		QueryPlayerAchievementsOptions options = new QueryPlayerAchievementsOptions
		{
			LocalUserId = EOSLoginManager.loggedInProductId,
			TargetUserId = EOSLoginManager.loggedInProductId
		};
		_achievementsInterface.QueryPlayerAchievements(ref options, null, null);
	}

	public override void AddAchievement(string achievementName)
	{
		if (!string.IsNullOrEmpty(achievementName))
		{
			achievementName = achievementName.ToUpper();
		}
		else
		{
			Debug.LogError("Invalid achievement name. Achievement is null or empty.");
		}
		if (IsValidAchievement(achievementName))
		{
			UnlockAchievementsOptions unlockAchievementsOptions = default(UnlockAchievementsOptions);
			unlockAchievementsOptions.AchievementIds = new Utf8String[1]
			{
				new Utf8String(achievementName)
			};
			unlockAchievementsOptions.UserId = EOSLoginManager.loggedInProductId;
			UnlockAchievementsOptions options = unlockAchievementsOptions;
			_achievementsInterface.UnlockAchievements(ref options, null, null);
		}
		else
		{
			Debug.LogError("Invalid achievement name: " + achievementName + ". Make sure the achievement's name is defined in the EOS Developer Portal.");
		}
	}

	private bool IsValidAchievement(string achievementName)
	{
		CopyAchievementDefinitionV2ByAchievementIdOptions copyAchievementDefinitionV2ByAchievementIdOptions = default(CopyAchievementDefinitionV2ByAchievementIdOptions);
		copyAchievementDefinitionV2ByAchievementIdOptions.AchievementId = achievementName;
		CopyAchievementDefinitionV2ByAchievementIdOptions options = copyAchievementDefinitionV2ByAchievementIdOptions;
		_achievementsInterface.CopyAchievementDefinitionV2ByAchievementId(ref options, out var outDefinition);
		if (outDefinition.HasValue)
		{
			return !outDefinition.Value.IsHidden;
		}
		return false;
	}
}

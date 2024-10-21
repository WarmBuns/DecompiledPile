using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2;

public class AchievementDef
{
	public AchievementIndex index;

	public ServerAchievementIndex serverIndex = new ServerAchievementIndex
	{
		intValue = -1
	};

	public string identifier;

	public string unlockableRewardIdentifier;

	public string prerequisiteAchievementIdentifier;

	public string nameToken;

	public string descriptionToken;

	public string iconPath;

	public uint lunarCoinReward;

	public Type type;

	public Type serverTrackerType;

	private static readonly string[] emptyStringArray = Array.Empty<string>();

	public string[] childAchievementIdentifiers = emptyStringArray;

	private Sprite achievedIcon;

	private Sprite unachievedIcon;

	public void SetAchievedIcon(Sprite icon)
	{
		achievedIcon = icon;
	}

	public void PreloadIcon()
	{
		AsyncOperationHandle<Sprite> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Sprite>(iconPath);
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Sprite> x)
		{
			achievedIcon = x.Result;
			if (!achievedIcon)
			{
				AsyncOperationHandle<Sprite> asyncOperationHandle2 = LegacyResourcesAPI.LoadAsync<Sprite>("Textures/AchievementIcons/texPlaceholderAchievement");
				asyncOperationHandle2.Completed += delegate(AsyncOperationHandle<Sprite> handle)
				{
					achievedIcon = handle.Result;
				};
			}
		};
	}

	public string GetReward()
	{
		return lunarCoinReward.ToString();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Sprite GetAchievedIcon()
	{
		if (!achievedIcon)
		{
			achievedIcon = LegacyResourcesAPI.Load<Sprite>(iconPath);
			if (!achievedIcon)
			{
				achievedIcon = LegacyResourcesAPI.Load<Sprite>("Textures/AchievementIcons/texPlaceholderAchievement");
			}
		}
		return achievedIcon;
	}

	public Sprite GetUnachievedIcon()
	{
		return LegacyResourcesAPI.Load<Sprite>("Textures/MiscIcons/texUnlockIcon");
	}

	public string GetAchievementSoundString()
	{
		if (unlockableRewardIdentifier.Contains("Characters."))
		{
			return "Play_UI_achievementUnlock_enhanced";
		}
		if (unlockableRewardIdentifier.Contains("Skills.") || unlockableRewardIdentifier.Contains("Skins."))
		{
			return "Play_UI_skill_unlock";
		}
		return "Play_UI_achievementUnlock";
	}
}

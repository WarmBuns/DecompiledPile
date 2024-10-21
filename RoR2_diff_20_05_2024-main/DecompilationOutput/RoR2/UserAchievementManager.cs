using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RoR2.Achievements;
using RoR2.Stats;
using UnityEngine;

namespace RoR2;

public class UserAchievementManager
{
	private readonly List<BaseAchievement> achievementsList = new List<BaseAchievement>();

	public LocalUser localUser;

	public UserProfile userProfile;

	public int dirtyGrantsCount;

	public int dirtyUpdatesCount;

	private readonly bool[] serverAchievementTrackingMask = new bool[AchievementManager.serverAchievementCount];

	private bool serverAchievementTrackingMaskDirty;

	public event Action<StatSheet> onUserProfileStatsUpdated;

	public event Action<StatSheet> onRunStatsUpdated;

	private void OnUserProfileStatsUpdated()
	{
		StatSheet statSheet = userProfile.statSheet;
		if (statSheet != null)
		{
			OnUserProfileStatsUpdated(statSheet);
		}
		PlayerStatsComponent cachedStatsComponent = localUser.cachedStatsComponent;
		if ((bool)cachedStatsComponent)
		{
			StatSheet currentStats = cachedStatsComponent.currentStats;
			if (currentStats != null)
			{
				OnRunStatsUpdated(currentStats);
			}
		}
	}

	private void OnUserProfileStatsUpdated([NotNull] StatSheet statSheet)
	{
		this.onUserProfileStatsUpdated?.Invoke(statSheet);
	}

	private void OnRunStatsUpdated([NotNull] StatSheet statSheet)
	{
		this.onRunStatsUpdated?.Invoke(statSheet);
	}

	public void SetServerAchievementTracked(ServerAchievementIndex serverAchievementIndex, bool shouldTrack)
	{
		if (serverAchievementTrackingMask[serverAchievementIndex.intValue] != shouldTrack)
		{
			serverAchievementTrackingMask[serverAchievementIndex.intValue] = shouldTrack;
			serverAchievementTrackingMaskDirty = true;
		}
	}

	public void TransmitAchievementRequestsToServer()
	{
		if ((bool)localUser.currentNetworkUser)
		{
			localUser.currentNetworkUser.GetComponent<ServerAchievementTracker>().SendAchievementTrackerRequestsMaskToServer(serverAchievementTrackingMask);
		}
	}

	public void Update()
	{
		if (serverAchievementTrackingMaskDirty)
		{
			serverAchievementTrackingMaskDirty = false;
			TransmitAchievementRequestsToServer();
		}
		int num = achievementsList.Count - 1;
		while (num >= 0 && dirtyGrantsCount > 0)
		{
			BaseAchievement baseAchievement = achievementsList[num];
			if (baseAchievement.shouldGrant)
			{
				dirtyGrantsCount--;
				achievementsList.RemoveAt(num);
				userProfile.AddAchievement(baseAchievement.achievementDef.identifier, isExternal: true);
				baseAchievement.OnGranted();
				baseAchievement.OnUninstall();
				NetworkUser currentNetworkUser = localUser.currentNetworkUser;
				if (currentNetworkUser != null)
				{
					currentNetworkUser.CallCmdReportAchievement(baseAchievement.achievementDef.nameToken);
				}
			}
			num--;
		}
	}

	public void GrantAchievement(AchievementDef achievementDef)
	{
		for (int i = 0; i < achievementsList.Count; i++)
		{
			if (achievementsList[i].achievementDef == achievementDef)
			{
				achievementsList[i].Grant();
			}
		}
	}

	public void HandleServerAchievementCompleted(ServerAchievementIndex serverAchievementIndex)
	{
		achievementsList.FirstOrDefault((BaseAchievement a) => a.achievementDef.serverIndex == serverAchievementIndex)?.Grant();
	}

	public void HandleServerTryToCompleteActivity(ServerAchievementIndex serverAchievementIndex)
	{
		achievementsList.FirstOrDefault((BaseAchievement a) => a.achievementDef.serverIndex == serverAchievementIndex)?.TryToCompleteActivity();
	}

	public float GetAchievementProgress(AchievementDef achievementDef)
	{
		return achievementsList.FirstOrDefault((BaseAchievement a) => a.achievementDef == achievementDef)?.ProgressForAchievement() ?? 1f;
	}

	public void OnInstall(LocalUser localUser)
	{
		this.localUser = localUser;
		userProfile = localUser.userProfile;
		UserProfile obj = userProfile;
		obj.onStatsReceived = (Action)Delegate.Combine(obj.onStatsReceived, new Action(OnUserProfileStatsUpdated));
		foreach (string readOnlyAchievementIdentifier in AchievementManager.readOnlyAchievementIdentifiers)
		{
			AchievementDef achievementDef = AchievementManager.GetAchievementDef(readOnlyAchievementIdentifier);
			if (userProfile.HasAchievement(readOnlyAchievementIdentifier))
			{
				if (!userProfile.HasUnlockable(achievementDef.unlockableRewardIdentifier))
				{
					Debug.LogFormat("UserProfile {0} has achievement {1} but not its unlockable {2}. Granting.", userProfile.name, achievementDef.nameToken, achievementDef.unlockableRewardIdentifier);
					userProfile.AddUnlockToken(achievementDef.unlockableRewardIdentifier);
				}
			}
			else
			{
				BaseAchievement baseAchievement = (BaseAchievement)Activator.CreateInstance(achievementDef.type);
				baseAchievement.achievementDef = achievementDef;
				baseAchievement.owner = this;
				achievementsList.Add(baseAchievement);
				baseAchievement.OnInstall();
			}
		}
	}

	public void OnUninstall()
	{
		for (int num = achievementsList.Count - 1; num >= 0; num--)
		{
			achievementsList[num].OnUninstall();
		}
		achievementsList.Clear();
		UserProfile obj = userProfile;
		obj.onStatsReceived = (Action)Delegate.Remove(obj.onStatsReceived, new Action(OnUserProfileStatsUpdated));
		userProfile = null;
		localUser = null;
	}
}

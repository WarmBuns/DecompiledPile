using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RoR2;
using RoR2.EntitlementManagement;
using RoR2.UI.MainMenu;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Assets.RoR2.Scripts.Platform;

public class ActivityManager
{
	protected ActivityData LoadedActivityData;

	protected Dictionary<string, BaseActivity> InProgressActivities = new Dictionary<string, BaseActivity>();

	protected Dictionary<string, BaseActivity> AvailableActivities = new Dictionary<string, BaseActivity>();

	protected BaseActivity ActivityToProcess;

	public bool IsInitialized { get; private set; }

	public BaseActivity.ActivityGameMode CurrentGameMode
	{
		get
		{
			GameModeIndex gameModeIndex = GameModeIndex.Invalid;
			if (null != PreGameController.instance)
			{
				gameModeIndex = PreGameController.instance.gameModeIndex;
			}
			if (null != Run.instance)
			{
				gameModeIndex = Run.instance.gameModeIndex;
			}
			if (GameModeIndex.Invalid != gameModeIndex)
			{
				if (gameModeIndex == GameModeCatalog.FindGameModeIndex("ClassicRun"))
				{
					return BaseActivity.ActivityGameMode.ClassicRun;
				}
				if (gameModeIndex == GameModeCatalog.FindGameModeIndex("EclipseRun"))
				{
					return BaseActivity.ActivityGameMode.Eclipse;
				}
				if (gameModeIndex == GameModeCatalog.FindGameModeIndex("InfiniteTowerRun"))
				{
					return BaseActivity.ActivityGameMode.InfiniteTower;
				}
				if (gameModeIndex == GameModeCatalog.FindGameModeIndex("WeeklyRun"))
				{
					return BaseActivity.ActivityGameMode.WeeklyRun;
				}
			}
			Debug.LogError("ActivityManager::CurrentGameMode property unable to determine GameMode.");
			return BaseActivity.ActivityGameMode.Any;
		}
	}

	public BaseActivity.ActivityGameType CurrentGameType
	{
		get
		{
			ReadOnlyCollection<NetworkUser> readOnlyLocalPlayersList = NetworkUser.readOnlyLocalPlayersList;
			if (readOnlyLocalPlayersList != null)
			{
				if (readOnlyLocalPlayersList.Count == 1)
				{
					return BaseActivity.ActivityGameType.SinglePlayer;
				}
				if (readOnlyLocalPlayersList.Count > 1)
				{
					return BaseActivity.ActivityGameType.MultiPlayer;
				}
			}
			Debug.LogError("ActivityManager::CurrentGameType property unable to determine GameType.");
			return BaseActivity.ActivityGameType.Any;
		}
	}

	public BaseActivity.ActivitySurvivor CurrentSurvivor
	{
		get
		{
			UserProfile userProfile = LocalUserManager.GetFirstLocalUser()?.userProfile;
			if (userProfile != null)
			{
				SurvivorDef survivorDef = userProfile?.GetSurvivorPreference();
				if (null != survivorDef)
				{
					BaseActivity.ActivitySurvivor result = BaseActivity.ActivitySurvivor.Any;
					if (Enum.TryParse<BaseActivity.ActivitySurvivor>(survivorDef.cachedName, out result))
					{
						return result;
					}
				}
			}
			Debug.LogError("ActivityManager::CurrentSurvivor property unable to determine survivor.");
			return BaseActivity.ActivitySurvivor.Any;
		}
	}

	private bool ValidCurrentSelectorExists
	{
		get
		{
			if ((null != PreGameController.instance || null != Run.instance) && LocalUserManager.GetFirstLocalUser()?.userProfile != null)
			{
				if (NetworkUser.readOnlyLocalPlayersList != null)
				{
					return NetworkUser.readOnlyLocalPlayersList.Count >= 0;
				}
				return false;
			}
			return false;
		}
	}

	public BaseActivitySelector CurrentActivitySelector
	{
		get
		{
			if (ValidCurrentSelectorExists)
			{
				return new BaseActivitySelector
				{
					activitySurvivor = CurrentSurvivor,
					activityGameMode = CurrentGameMode,
					activityGameType = CurrentGameType
				};
			}
			return null;
		}
	}

	public virtual void Initialize()
	{
		MainMenuController.OnPreMainMenuInitialized -= OnPreMainMenuInitializedCallback;
		MainMenuController.OnPreMainMenuInitialized += OnPreMainMenuInitializedCallback;
		LocalUserManager.onUserSignIn -= OnLocalUserSignIn;
		LocalUserManager.onUserSignIn += OnLocalUserSignIn;
		BaseUserEntitlementTracker<LocalUser>.OnAllUserEntitlementsUpdated = (Action)Delegate.Remove(BaseUserEntitlementTracker<LocalUser>.OnAllUserEntitlementsUpdated, new Action(OnEntitlementsChangedCallback));
		BaseUserEntitlementTracker<LocalUser>.OnAllUserEntitlementsUpdated = (Action)Delegate.Combine(BaseUserEntitlementTracker<LocalUser>.OnAllUserEntitlementsUpdated, new Action(OnEntitlementsChangedCallback));
		Run.onRunStartGlobal += OnRunStartGlobal;
		Run.onClientGameOverGlobal += OnClientGameOverGlobal;
	}

	protected virtual void OnPreMainMenuReInitializedCallback()
	{
		AbandonInProgressActivities();
	}

	protected virtual void OnLocalUserSignIn(LocalUser localUser)
	{
		if (localUser.userProfile != null && localUser.userProfile == LocalUserManager.GetFirstLocalUser().userProfile)
		{
			RebuildActivityAvailabilityForUser(localUser);
			IsInitialized = true;
		}
	}

	protected virtual void AbandonInProgressActivities()
	{
		BaseActivitySelector activitySelector = new BaseActivitySelector();
		PlatformSystems.activityManager.TryToAbandonActivity(activitySelector);
	}

	protected virtual void OnClientGameOverGlobal(Run run, RunReport runReport)
	{
		if (runReport.gameEnding == RoR2Content.GameEndings.StandardLoss)
		{
			if (ValidCurrentSelectorExists)
			{
				PlatformSystems.activityManager.TryToFailActivity(PlatformSystems.activityManager.CurrentActivitySelector);
			}
			else
			{
				Debug.LogError("ActivityManager::OnClientGameOverGlobal - missing context to fail activities activities on Game Over.");
			}
		}
	}

	protected virtual void OnRunStartGlobal(Run run)
	{
		if (ValidCurrentSelectorExists)
		{
			TryToStartActivity(CurrentActivitySelector);
		}
		else
		{
			Debug.LogError("ActivityManager::OnRunStartGlobal - missing context to start activities on Run start.");
		}
	}

	protected virtual void OnPreMainMenuInitializedCallback()
	{
		MainMenuController.OnPreMainMenuInitialized -= OnPreMainMenuInitializedCallback;
		AsyncOperationHandle<ActivityData> handle = Addressables.LoadAssetAsync<ActivityData>("RoR2/Base/Common/ActivityData.asset");
		ActivityData loadedActivityData = handle.WaitForCompletion();
		LoadedActivityData = loadedActivityData;
		Addressables.Release(handle);
		if (GameIntentReceiver.GameIntentAdapter.hasPendingGameIntent)
		{
			CreateStartupActivity();
		}
		GameIntentReceiver.GameIntentAdapter.OnGameIntentReceivedEvent -= GameIntentReceiverListener;
		GameIntentReceiver.GameIntentAdapter.OnGameIntentReceivedEvent += GameIntentReceiverListener;
		if (ActivityToProcess != null)
		{
			RoR2Application.instance.StartCoroutine(ProcessGameActivity());
			GameIntentReceiver.GameIntentAdapter.ClearPendingGameIntent();
		}
		MainMenuController.OnPreMainMenuInitialized += OnPreMainMenuReInitializedCallback;
	}

	protected virtual void CreateStartupActivity()
	{
	}

	public virtual void GameIntentReceiverListener(GameIntentAdapter.EventResponse eventResponseObject)
	{
		eventResponseObject.EventHasBeenHandled(eventHandled: false);
	}

	public virtual void TryToStartActivity(BaseActivitySelector activitySelector)
	{
	}

	public virtual void TryToCompleteActivity(BaseActivitySelector activitySelector, bool markUnavailableOnComplete = true, bool applyAsDelta = true)
	{
	}

	public virtual void TryToAbandonActivity(BaseActivitySelector activitySelector)
	{
	}

	public virtual void TryToFailActivity(BaseActivitySelector activitySelector)
	{
	}

	public virtual void TryToSetActivityAvailability(BaseActivitySelector activitySelector, bool newAvailability, bool applyAsDelta = true, bool sendImmediate = true)
	{
	}

	public virtual void FlushAvailabilityUpdates(bool applyAsDelta)
	{
	}

	public virtual bool ShouldSkipGameIntentHandler(BaseActivity activity)
	{
		return CurrentActivitySelector?.IsCompatibleWith(activity) ?? false;
	}

	public virtual bool IsActivityAvailable(BaseActivity baseActivity)
	{
		return AvailableActivities.ContainsKey(baseActivity.ActivityID);
	}

	protected IEnumerator ProcessGameActivity()
	{
		if (!IsInitialized)
		{
			yield return new WaitForEndOfFrame();
		}
		BaseActivity gameActivity = LoadedActivityData.LookupActivityByID(ActivityToProcess.ActivityID);
		if (gameActivity == null)
		{
			Debug.LogError("ActivityManager::ProcessGameActivity() was unable to find data for a Game Activity!");
			yield break;
		}
		if (!IsActivityAvailable(gameActivity))
		{
			Debug.LogError("ActivityManager::ProcessGameActivity() user attempted to launch an unavailable activity!");
			yield break;
		}
		if (!RoR2Application.IsMainScene)
		{
			if (ShouldSkipGameIntentHandler(gameActivity))
			{
				yield break;
			}
			global::RoR2.Console.instance.SubmitCmd(null, "transition_command \"disconnect\";");
			yield return new WaitForSeconds(1f);
			global::RoR2.Console.instance.SubmitCmd(null, "transition_command \"set_scene title\";");
			while (!RoR2Application.IsMainScene)
			{
				yield return new WaitForEndOfFrame();
			}
		}
		while (!RoR2Application.IsMainMenuInitialized)
		{
			yield return new WaitForEndOfFrame();
		}
		string gameModeStr;
		if (gameActivity.GameMode == BaseActivity.ActivityGameMode.ClassicRun || gameActivity.GameMode == BaseActivity.ActivityGameMode.Any)
		{
			gameModeStr = "ClassicRun";
		}
		else if (gameActivity.GameMode == BaseActivity.ActivityGameMode.WeeklyRun)
		{
			gameModeStr = "WeeklyRun";
		}
		else if (gameActivity.GameMode == BaseActivity.ActivityGameMode.Eclipse)
		{
			gameModeStr = "EclipseRun";
		}
		else if (gameActivity.GameMode == BaseActivity.ActivityGameMode.InfiniteTower)
		{
			gameModeStr = "InfiniteTowerRun";
		}
		else
		{
			Debug.LogError("ActivityManager::ProcessGameActivity() - Trying to transition to gamemode with unhandled BaseActivity.ActivityGameMode. Transitioning to Classic Run...");
			gameModeStr = "ClassicRun";
		}
		while (TransitionCommand.requestPending)
		{
			yield return new WaitForEndOfFrame();
		}
		global::RoR2.Console.instance.SubmitCmd(null, "transition_command \"gamemode " + gameModeStr + "; host 0;\"");
		string text = gameActivity.Survivor.ToString();
		if (string.Compare("Any", text) != 0)
		{
			SurvivorIndex survivorIndex = SurvivorCatalog.FindSurvivorIndex(text);
			UserProfile userProfile = LocalUserManager.GetFirstLocalUser()?.userProfile;
			if (userProfile.HasSurvivorUnlocked(survivorIndex))
			{
				SurvivorDef survivorDef = SurvivorCatalog.GetSurvivorDef(survivorIndex);
				userProfile.SetSurvivorPreference(survivorDef);
			}
		}
		yield return new WaitForSeconds(2f);
		RoR2Application.SetMapsEnabled(enabled: true);
	}

	public void RebuildActivityAvailabilityForUser(LocalUser localUser)
	{
		if (LoadedActivityData == null)
		{
			return;
		}
		UserProfile userProfile = localUser.userProfile;
		BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
		List<BaseActivity> list = LoadedActivityData.CompareActivityCriteria(baseActivitySelector);
		foreach (BaseActivity item in list)
		{
			if (item.AchievementID.Length == 0)
			{
				continue;
			}
			baseActivitySelector.activityAchievementID = item.AchievementID;
			bool flag = userProfile.CanSeeAchievement(item.AchievementID);
			bool flag2 = userProfile.HasAchievement(item.AchievementID);
			if (item.RequiredEntitlementID.Length > 0)
			{
				if (EntitlementCatalog.FindEntitlementIndex(item.RequiredEntitlementID) == EntitlementIndex.None)
				{
					Debug.LogError("ActivityManager::RebuildAvailabilityForUser - Activity requires entitlement (" + item.RequiredEntitlementID + ") that is not defined in the EntitlementCatalog. (probably a typo)");
				}
				else
				{
					EntitlementDef entitlementDef = EntitlementCatalog.GetEntitlementDef(EntitlementCatalog.FindEntitlementIndex(item.RequiredEntitlementID));
					if (!EntitlementManager.localUserEntitlementTracker.UserHasEntitlement(localUser, entitlementDef))
					{
						flag = false;
					}
				}
			}
			TryToSetActivityAvailability(baseActivitySelector, flag && !flag2, applyAsDelta: false, sendImmediate: false);
		}
		if (list.Count > 0)
		{
			FlushAvailabilityUpdates(applyAsDelta: false);
		}
	}

	private void OnEntitlementsChangedCallback()
	{
		RebuildActivityAvailabilityForUser(LocalUserManager.GetFirstLocalUser());
	}
}

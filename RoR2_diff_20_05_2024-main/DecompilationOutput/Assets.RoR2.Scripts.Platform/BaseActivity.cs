using System;
using UnityEngine;

namespace Assets.RoR2.Scripts.Platform;

[Serializable]
public class BaseActivity
{
	public enum ActivityGameType : uint
	{
		SinglePlayer,
		MultiPlayer,
		Any
	}

	public enum ActivityGameMode : uint
	{
		ClassicRun,
		Eclipse,
		InfiniteTower,
		WeeklyRun,
		Any
	}

	public enum ActivitySurvivor : uint
	{
		Bandit2,
		Captain,
		Commando,
		Croco,
		Engi,
		Heretic,
		Huntress,
		Loader,
		Mage,
		Merc,
		Toolbot,
		Treebot,
		Railgunner,
		Any
	}

	[SerializeField]
	protected ActivityGameType _gameType;

	[SerializeField]
	protected ActivityGameMode _gameMode;

	[SerializeField]
	protected ActivitySurvivor _survivor;

	[SerializeField]
	protected string _activityID;

	[SerializeField]
	protected string _achievementID;

	[SerializeField]
	protected string _requiredEntitlementID;

	public ActivityGameType GameType => _gameType;

	public ActivityGameMode GameMode => _gameMode;

	public ActivitySurvivor Survivor => _survivor;

	public string ActivityID => _activityID;

	public string AchievementID => _achievementID;

	public string RequiredEntitlementID => _requiredEntitlementID;

	public virtual void StartActivity()
	{
		Debug.LogError("BaseActivity::StartActivity() invoked!");
	}

	public virtual void CompleteActivity()
	{
		Debug.LogError("BaseActivity::CompleteActivity() invoked!");
	}

	public virtual void AbandonActivity()
	{
		Debug.LogError("BaseActivity::AbandonActivity() invoked!");
	}

	public virtual void FailActivity()
	{
		Debug.LogError("BaseActivity::FailActivity() invoked!");
	}
}

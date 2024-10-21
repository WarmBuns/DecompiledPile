using System;
using RoR2.Stats;

namespace RoR2.Achievements;

[RegisterAchievement("Complete20Stages", "Items.Clover", null, 3u, null)]
public class Complete20StagesAchievement : BaseAchievement
{
	private const int requirement = 20;

	private bool listeningForStats;

	private UserProfile subscribedProfile;

	private MemoizedGetComponent<PlayerStatsComponent> playerStatsComponentGetter;

	public override void OnInstall()
	{
		base.OnInstall();
		Run.onRunStartGlobal += OnRunStart;
	}

	public override void OnUninstall()
	{
		Run.onRunStartGlobal -= OnRunStart;
		SetListeningForStats(shouldListen: false);
		base.OnUninstall();
	}

	private void OnRunStart(Run run)
	{
		SetListeningForStats(shouldListen: true);
	}

	private void SetListeningForStats(bool shouldListen)
	{
		if (listeningForStats != shouldListen)
		{
			listeningForStats = shouldListen;
			if (listeningForStats)
			{
				subscribedProfile = base.localUser.userProfile;
				UserProfile obj = subscribedProfile;
				obj.onStatsReceived = (Action)Delegate.Combine(obj.onStatsReceived, new Action(OnStatsReceived));
			}
			else
			{
				UserProfile obj2 = subscribedProfile;
				obj2.onStatsReceived = (Action)Delegate.Remove(obj2.onStatsReceived, new Action(OnStatsReceived));
				subscribedProfile = null;
			}
		}
	}

	private void OnStatsReceived()
	{
		PlayerStatsComponent playerStatsComponent = playerStatsComponentGetter.Get(base.localUser.cachedMasterObject);
		if ((bool)playerStatsComponent && playerStatsComponent.currentStats.GetStatValueULong(StatDef.highestStagesCompleted) >= 20)
		{
			Grant();
		}
	}
}

using System;
using RoR2.Stats;

namespace RoR2.Achievements;

[RegisterAchievement("KillBossQuantityInRun", "Items.LunarSkillReplacements", null, 2u, null)]
public class KillBossQuantityInRunAchievement : BaseAchievement
{
	private static readonly ulong requirement = 15uL;

	private PlayerStatsComponent playerStatsComponent;

	public override void OnInstall()
	{
		base.OnInstall();
		base.localUser.onMasterChanged += OnMasterChanged;
		UserProfile obj = base.userProfile;
		obj.onStatsReceived = (Action)Delegate.Combine(obj.onStatsReceived, new Action(OnStatsReceived));
	}

	public override void OnUninstall()
	{
		UserProfile obj = base.userProfile;
		obj.onStatsReceived = (Action)Delegate.Remove(obj.onStatsReceived, new Action(OnStatsReceived));
		base.localUser.onMasterChanged -= OnMasterChanged;
		base.OnUninstall();
	}

	private void OnMasterChanged()
	{
		playerStatsComponent = base.localUser.cachedMasterController?.GetComponent<PlayerStatsComponent>();
	}

	private void OnStatsReceived()
	{
		Check();
	}

	private void Check()
	{
		if (playerStatsComponent != null && requirement <= playerStatsComponent.currentStats.GetStatValueULong(StatDef.totalTeleporterBossKillsWitnessed))
		{
			Grant();
		}
	}
}

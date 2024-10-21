using System;
using RoR2.Stats;
using UnityEngine;

namespace RoR2.Achievements;

[RegisterAchievement("SuicideHermitCrabs", "Items.AutoCastEquipment", null, 2u, typeof(SuicideHermitCrabsServerAchievement))]
public class SuicideHermitCrabsAchievement : BaseAchievement
{
	private class SuicideHermitCrabsServerAchievement : BaseServerAchievement
	{
		private BodyIndex crabBodyIndex;

		public override void OnInstall()
		{
			base.OnInstall();
			crabBodyIndex = BodyCatalog.FindBodyIndex("HermitCrabBody");
			GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
		}

		public override void OnUninstall()
		{
			GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeath;
			base.OnUninstall();
		}

		private void OnCharacterDeath(DamageReport damageReport)
		{
			if (!damageReport.victimBody || damageReport.victimBody.bodyIndex != crabBodyIndex || damageReport.victimBody.teamComponent.teamIndex == TeamIndex.Player)
			{
				return;
			}
			GameObject inflictor = damageReport.damageInfo.inflictor;
			if ((bool)inflictor && (bool)inflictor.GetComponent<MapZone>())
			{
				PlayerStatsComponent masterPlayerStatsComponent = base.networkUser.masterPlayerStatsComponent;
				if ((bool)masterPlayerStatsComponent)
				{
					masterPlayerStatsComponent.currentStats.PushStatValue(StatDef.suicideHermitCrabsAchievementProgress, 1uL);
				}
			}
		}
	}

	private const int requirement = 20;

	public override void OnInstall()
	{
		base.OnInstall();
		UserProfile obj = base.userProfile;
		obj.onStatsReceived = (Action)Delegate.Combine(obj.onStatsReceived, new Action(Check));
		SetServerTracked(shouldTrack: true);
	}

	public override void OnUninstall()
	{
		UserProfile obj = base.userProfile;
		obj.onStatsReceived = (Action)Delegate.Remove(obj.onStatsReceived, new Action(Check));
		base.OnUninstall();
	}

	private void Check()
	{
		if (base.userProfile.statSheet.GetStatValueULong(StatDef.suicideHermitCrabsAchievementProgress) >= 20)
		{
			Grant();
		}
	}

	public override float ProgressForAchievement()
	{
		return (float)base.userProfile.statSheet.GetStatValueULong(StatDef.suicideHermitCrabsAchievementProgress) / 20f;
	}
}

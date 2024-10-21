using System;
using RoR2.Stats;

namespace RoR2.Achievements.Chef;

[RegisterAchievement("BarbecueQuantityBisonInRun", "Skills.Chef.YesChef", "ActivateChef", 3u, typeof(ChefAlternateSpecialBarbecueBisonServer))]
public class ChefAlternateSpecialBarbecueBison : BaseAchievement
{
	private class ChefAlternateSpecialBarbecueBisonServer : BaseServerAchievement
	{
		public override void OnInstall()
		{
			base.OnInstall();
			GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
		}

		public override void OnUninstall()
		{
			GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeath;
			base.OnUninstall();
		}

		private void OnCharacterDeath(DamageReport damageReport)
		{
			if ((object)damageReport.attackerMaster == base.networkUser.master && (object)damageReport.attackerMaster != null && damageReport.victimBody.baseNameToken == "BISON_BODY_NAME" && damageReport.victimBody.HasBuff(DLC2Content.Buffs.CookingFlambe) && damageReport.victimBody.HasBuff(DLC2Content.Buffs.CookingRoasted))
			{
				PlayerStatsComponent masterPlayerStatsComponent = base.networkUser.masterPlayerStatsComponent;
				if ((bool)masterPlayerStatsComponent)
				{
					masterPlayerStatsComponent.currentStats.PushStatValue(StatDef.chefAlternateSpecialBarbecueBisonAchievementProgress, 1uL);
				}
			}
		}
	}

	private ulong requirement = 10uL;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("ChefBody");
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		SetServerTracked(shouldTrack: true);
		UserProfile obj = base.userProfile;
		obj.onStatsReceived = (Action)Delegate.Combine(obj.onStatsReceived, new Action(Check));
	}

	protected override void OnBodyRequirementBroken()
	{
		SetServerTracked(shouldTrack: false);
		base.OnBodyRequirementBroken();
		UserProfile obj = base.userProfile;
		obj.onStatsReceived = (Action)Delegate.Remove(obj.onStatsReceived, new Action(Check));
	}

	private void Check()
	{
		if (base.userProfile.statSheet.GetStatValueULong(StatDef.chefAlternateSpecialBarbecueBisonAchievementProgress) >= requirement)
		{
			Grant();
		}
	}

	public override float ProgressForAchievement()
	{
		return (float)base.userProfile.statSheet.GetStatValueULong(StatDef.chefAlternateSpecialBarbecueBisonAchievementProgress) / (float)requirement;
	}
}

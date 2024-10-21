using Assets.RoR2.Scripts.Platform;

namespace RoR2.Achievements.Bandit2;

[RegisterAchievement("Bandit2StackSuperBleed", "Skills.Bandit2.SerratedShivs", "CompleteThreeStages", 3u, typeof(Bandit2StackSuperBleedServerAchievement))]
public class Bandit2StackSuperBleedAchievement : BaseAchievement
{
	private class Bandit2StackSuperBleedServerAchievement : BaseServerAchievement
	{
		public override void OnInstall()
		{
			base.OnInstall();
			GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeathGlobal;
		}

		public override void OnUninstall()
		{
			GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeathGlobal;
			base.OnUninstall();
		}

		private void OnCharacterDeathGlobal(DamageReport damageReport)
		{
			if ((bool)damageReport.attackerBody && (object)damageReport.attackerBody == GetCurrentBody() && (bool)damageReport.victimBody && damageReport.victimBody.GetBuffCount(RoR2Content.Buffs.SuperBleed) >= requirement)
			{
				Grant();
				ServerTryToCompleteActivity();
			}
		}
	}

	private static readonly int requirement = 20;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("Bandit2Body");
	}

	protected override void OnBodyRequirementMet()
	{
		base.OnBodyRequirementMet();
		SetServerTracked(shouldTrack: true);
	}

	protected override void OnBodyRequirementBroken()
	{
		SetServerTracked(shouldTrack: false);
		base.OnBodyRequirementBroken();
	}

	public override void TryToCompleteActivity()
	{
		bool flag = base.localUser.id == LocalUserManager.GetFirstLocalUser().id;
		if (shouldGrant && flag)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "Bandit2StackSuperBleed";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}
}

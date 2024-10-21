using EntityStates.FalseSonBoss;

namespace RoR2.Achievements.Bandit2;

[RegisterAchievement("Bandit2ClearMeridianEvent", "Skins.Bandit2.Alt2", "CompleteThreeStages", 5u, typeof(Bandit2MeridianEventTriggerServerAchievement))]
public class Bandit2MeridianEventTriggerAchievement : BaseAchievement
{
	private class Bandit2MeridianEventTriggerServerAchievement : BaseServerAchievement
	{
		public override void OnInstall()
		{
			base.OnInstall();
			SkyJumpDeathState.falseSonDeathEvent += OnMeridianEventTriggerActivated;
		}

		public override void OnUninstall()
		{
			base.OnUninstall();
			SkyJumpDeathState.falseSonDeathEvent -= OnMeridianEventTriggerActivated;
		}

		private void OnMeridianEventTriggerActivated()
		{
			Grant();
		}
	}

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
}

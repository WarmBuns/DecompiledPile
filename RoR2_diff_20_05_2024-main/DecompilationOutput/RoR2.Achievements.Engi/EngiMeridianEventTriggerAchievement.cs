using EntityStates.FalseSonBoss;

namespace RoR2.Achievements.Engi;

[RegisterAchievement("EngiClearMeridianEvent", "Skins.Engi.Alt2", "Complete30StagesCareer", 5u, typeof(EngiMeridianEventTriggerServerAchievement))]
public class EngiMeridianEventTriggerAchievement : BaseAchievement
{
	private class EngiMeridianEventTriggerServerAchievement : BaseServerAchievement
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
		return BodyCatalog.FindBodyIndex("EngiBody");
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

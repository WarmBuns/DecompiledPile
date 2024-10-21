using EntityStates.FalseSonBoss;

namespace RoR2.Achievements.Loader;

[RegisterAchievement("LoaderClearMeridianEvent", "Skins.Loader.Alt2", "DefeatSuperRoboBallBoss", 5u, typeof(LoaderMeridianEventTriggerServerAchievement))]
public class LoaderMeridianEventTriggerAchievement : BaseAchievement
{
	private class LoaderMeridianEventTriggerServerAchievement : BaseServerAchievement
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
		return BodyCatalog.FindBodyIndex("LoaderBody");
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

using EntityStates.FalseSonBoss;

namespace RoR2.Achievements.Mage;

[RegisterAchievement("MageClearMeridianEvent", "Skins.Mage.Alt2", "FreeMage", 5u, typeof(MageMeridianEventTriggerServerAchievement))]
public class MageMeridianEventTriggerAchievement : BaseAchievement
{
	private class MageMeridianEventTriggerServerAchievement : BaseServerAchievement
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
		return BodyCatalog.FindBodyIndex("MageBody");
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

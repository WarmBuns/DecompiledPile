using EntityStates.FalseSonBoss;

namespace RoR2.Achievements.Toolbot;

[RegisterAchievement("ToolbotClearMeridianEvent", "Skins.Toolbot.Alt2", "RepeatFirstTeleporter", 5u, typeof(ToolbotMeridianEventTriggerServerAchievement))]
public class ToolbotMeridianEventTriggerAchievement : BaseAchievement
{
	private class ToolbotMeridianEventTriggerServerAchievement : BaseServerAchievement
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
		return BodyCatalog.FindBodyIndex("ToolbotBody");
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

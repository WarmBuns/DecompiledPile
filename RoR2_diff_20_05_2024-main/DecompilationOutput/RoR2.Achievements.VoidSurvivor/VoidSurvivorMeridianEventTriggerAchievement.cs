using EntityStates.FalseSonBoss;

namespace RoR2.Achievements.VoidSurvivor;

[RegisterAchievement("VoidSurvivorClearMeridianEvent", "Skins.VoidSurvivor.Alt2", "CompleteVoidEnding", 3u, typeof(VoidSurvivorMeridianEventTriggerServerAchievement))]
public class VoidSurvivorMeridianEventTriggerAchievement : BaseAchievement
{
	private class VoidSurvivorMeridianEventTriggerServerAchievement : BaseServerAchievement
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
		return BodyCatalog.FindBodyIndex("VoidSurvivorBody");
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

using EntityStates.Treebot.UnlockInteractable;

namespace RoR2.Achievements;

[RegisterAchievement("RescueTreebot", "Characters.Treebot", null, 3u, typeof(RescueTreebotServerAchievement))]
public class RescueTreebotAchievement : BaseAchievement
{
	public class RescueTreebotServerAchievement : BaseServerAchievement
	{
		public override void OnInstall()
		{
			base.OnInstall();
			Unlock.onActivated += OnActivated;
		}

		public override void OnUninstall()
		{
			Unlock.onActivated -= OnActivated;
			base.OnInstall();
		}

		private void OnActivated(Interactor interactor)
		{
			CharacterBody currentBody = serverAchievementTracker.networkUser.GetCurrentBody();
			if ((bool)currentBody && currentBody.GetComponent<Interactor>() == interactor)
			{
				Grant();
			}
		}
	}

	public override void OnInstall()
	{
		base.OnInstall();
		SetServerTracked(shouldTrack: true);
	}

	public override void OnUninstall()
	{
		base.OnUninstall();
	}
}

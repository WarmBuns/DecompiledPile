namespace RoR2.Achievements.Chef;

[RegisterAchievement("ActivateChef", "Characters.Chef", null, 3u, typeof(ActivateChefServerAchievement))]
public class ActivateChefAchievement : BaseAchievement
{
	private class ActivateChefServerAchievement : BaseServerAchievement
	{
		public override void OnInstall()
		{
			base.OnInstall();
			WokController.chefRecipeComplete += OnRecipeComplete;
		}

		public override void OnUninstall()
		{
			base.OnInstall();
			WokController.chefRecipeComplete -= OnRecipeComplete;
		}

		private void OnRecipeComplete(Interactor interactor)
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

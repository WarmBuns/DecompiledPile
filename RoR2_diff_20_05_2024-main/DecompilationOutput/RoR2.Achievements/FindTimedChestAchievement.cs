using EntityStates.TimedChest;

namespace RoR2.Achievements;

[RegisterAchievement("FindTimedChest", "Items.BFG", null, 2u, typeof(FindTimedChestServerAchievement))]
public class FindTimedChestAchievement : BaseAchievement
{
	private class FindTimedChestServerAchievement : BaseServerAchievement
	{
		public override void OnInstall()
		{
			base.OnInstall();
			Opening.onOpened += OnOpened;
		}

		public override void OnUninstall()
		{
			base.OnInstall();
			Opening.onOpened -= OnOpened;
		}

		private void OnOpened()
		{
			Grant();
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

namespace RoR2.Achievements;

[RegisterAchievement("MultiCombatShrine", "Items.EnergizedOnEquipmentUse", null, 2u, typeof(MultiCombatShrineServerAchievement))]
public class MultiCombatShrineAchievement : BaseAchievement
{
	private class MultiCombatShrineServerAchievement : BaseServerAchievement
	{
		private int counter;

		private static readonly int requirement = 3;

		public override void OnInstall()
		{
			base.OnInstall();
			ShrineCombatBehavior.onDefeatedServerGlobal += OnShrineDefeated;
			Stage.onServerStageBegin += OnServerStageBegin;
		}

		public override void OnUninstall()
		{
			Stage.onServerStageBegin -= OnServerStageBegin;
			ShrineCombatBehavior.onDefeatedServerGlobal -= OnShrineDefeated;
			base.OnUninstall();
		}

		private void OnServerStageBegin(Stage stage)
		{
			counter = 0;
		}

		private void OnShrineDefeated(ShrineCombatBehavior instance)
		{
			counter++;
			Check();
		}

		private void Check()
		{
			if (counter >= requirement)
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
		SetServerTracked(shouldTrack: false);
		base.OnUninstall();
	}
}

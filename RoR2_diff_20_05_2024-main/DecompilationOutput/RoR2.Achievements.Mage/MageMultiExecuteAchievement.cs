namespace RoR2.Achievements.Mage;

public class MageMultiExecuteAchievement : BaseAchievement
{
	private class MageMultiExecuteServerAchievement : BaseServerAchievement
	{
		private DoXInYSecondsTracker tracker;

		public override void OnInstall()
		{
			base.OnInstall();
			tracker = new DoXInYSecondsTracker(requirement, window);
			Run.onRunStartGlobal += OnRunStart;
			GlobalEventManager.onServerCharacterExecuted += OnServerCharacterExecuted;
		}

		public override void OnUninstall()
		{
			Run.onRunStartGlobal -= OnRunStart;
			GlobalEventManager.onServerCharacterExecuted -= OnServerCharacterExecuted;
			base.OnUninstall();
		}

		private void OnRunStart(Run run)
		{
			tracker.Clear();
		}

		private void OnServerCharacterExecuted(DamageReport damageReport, float executionHealthLost)
		{
			if ((object)damageReport.attackerMaster == base.networkUser.master && (object)base.networkUser.master != null && tracker.Push(Run.FixedTimeStamp.now.t))
			{
				Grant();
			}
		}
	}

	private static int requirement = 10;

	private static float window = 10f;

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

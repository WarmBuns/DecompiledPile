namespace RoR2.Achievements;

public abstract class BaseEndingAchievement : BaseAchievement
{
	protected abstract bool ShouldGrant(RunReport runReport);

	public override void OnInstall()
	{
		base.OnInstall();
		Run.onClientGameOverGlobal += OnClientGameOverGlobal;
	}

	public override void OnUninstall()
	{
		Run.onClientGameOverGlobal -= OnClientGameOverGlobal;
		base.OnUninstall();
	}

	protected virtual void OnClientGameOverGlobal(Run run, RunReport runReport)
	{
		if (ShouldGrant(runReport))
		{
			Grant();
		}
		TryToCompleteActivity();
	}
}

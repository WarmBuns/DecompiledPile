using EntityStates.FalseSonBoss;

namespace RoR2.Achievements;

public class MeridianEventTriggerAchievement : BaseAchievement
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
		if (base.isUserAlive)
		{
			Grant();
		}
	}
}

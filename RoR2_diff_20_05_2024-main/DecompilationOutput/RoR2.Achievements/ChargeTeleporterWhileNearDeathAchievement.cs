namespace RoR2.Achievements;

[RegisterAchievement("ChargeTeleporterWhileNearDeath", "Items.WarCryOnMultiKill", null, 2u, null)]
public class ChargeTeleporterWhileNearDeathAchievement : BaseAchievement
{
	private const float requirement = 0.1f;

	public override void OnInstall()
	{
		base.OnInstall();
		TeleporterInteraction.onTeleporterChargedGlobal += OnTeleporterCharged;
	}

	public override void OnUninstall()
	{
		TeleporterInteraction.onTeleporterChargedGlobal -= OnTeleporterCharged;
		base.OnUninstall();
	}

	private void OnTeleporterCharged(TeleporterInteraction teleporterInteraction)
	{
		Check();
	}

	private void Check()
	{
		if ((bool)base.localUser.cachedBody && (bool)base.localUser.cachedBody.healthComponent && base.localUser.cachedBody.healthComponent.alive && base.localUser.cachedBody.healthComponent.combinedHealthFraction <= 0.1f)
		{
			Grant();
		}
	}
}

namespace RoR2.Achievements;

[RegisterAchievement("CompleteTeleporterWithoutInjury", "Items.SecondarySkillMagazine", null, 1u, null)]
public class CompleteTeleporterWithoutInjuryAchievement : BaseAchievement
{
	private bool hasBeenHit;

	public override void OnInstall()
	{
		base.OnInstall();
		TeleporterInteraction.onTeleporterBeginChargingGlobal += OnTeleporterBeginCharging;
		TeleporterInteraction.onTeleporterChargedGlobal += OnTeleporterCharged;
		GlobalEventManager.onClientDamageNotified += OnClientDamageNotified;
	}

	public override void OnUninstall()
	{
		TeleporterInteraction.onTeleporterBeginChargingGlobal -= OnTeleporterBeginCharging;
		TeleporterInteraction.onTeleporterChargedGlobal -= OnTeleporterCharged;
		GlobalEventManager.onClientDamageNotified -= OnClientDamageNotified;
		base.OnUninstall();
	}

	private void OnTeleporterBeginCharging(TeleporterInteraction teleporterInteraction)
	{
		hasBeenHit = false;
	}

	private void OnTeleporterCharged(TeleporterInteraction teleporterInteraction)
	{
		Check();
	}

	private void OnClientDamageNotified(DamageDealtMessage damageDealtMessage)
	{
		if (!hasBeenHit && (bool)damageDealtMessage.victim && damageDealtMessage.victim == base.localUser.cachedBodyObject)
		{
			hasBeenHit = true;
		}
	}

	private void Check()
	{
		if ((bool)base.localUser.cachedBody && (bool)base.localUser.cachedBody.healthComponent && base.localUser.cachedBody.healthComponent.alive && !hasBeenHit)
		{
			Grant();
		}
	}
}

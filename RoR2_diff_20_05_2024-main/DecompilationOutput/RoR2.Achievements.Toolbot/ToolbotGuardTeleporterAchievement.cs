namespace RoR2.Achievements.Toolbot;

[RegisterAchievement("ToolbotGuardTeleporter", "Skills.Toolbot.Grenade", "RepeatFirstTeleporter", 3u, typeof(ToolbotGuardTeleporterServerAchievement))]
public class ToolbotGuardTeleporterAchievement : BaseAchievement
{
	public class ToolbotGuardTeleporterServerAchievement : BaseServerAchievement
	{
		private bool stayedInZone;

		private int killCount;

		private int killRequirement = 2;

		private BodyIndex beetleQueenBodyIndex = BodyIndex.None;

		private Run.FixedTimeStamp teleporterStartChargingTime = Run.FixedTimeStamp.negativeInfinity;

		private static readonly float gracePeriod = 2f;

		public override void OnInstall()
		{
			base.OnInstall();
			TeleporterInteraction.onTeleporterBeginChargingGlobal += OnTeleporterBeginCharging;
			TeleporterInteraction.onTeleporterChargedGlobal += OnTeleporterCharged;
			killCount = 0;
			beetleQueenBodyIndex = BodyCatalog.FindBodyIndex("BeetleQueen2Body");
		}

		public override void OnUninstall()
		{
			TeleporterInteraction.onTeleporterChargedGlobal -= OnTeleporterCharged;
			TeleporterInteraction.onTeleporterBeginChargingGlobal -= OnTeleporterBeginCharging;
			SetStayedInZone(newStayedInZone: false);
			base.OnUninstall();
		}

		private void OnTeleporterBeginCharging(TeleporterInteraction teleporterInteraction)
		{
			teleporterStartChargingTime = Run.FixedTimeStamp.now;
			SetStayedInZone(newStayedInZone: true);
		}

		private void OnTeleporterCharged(TeleporterInteraction teleporterInteraction)
		{
			SetStayedInZone(newStayedInZone: false);
		}

		private void SetStayedInZone(bool newStayedInZone)
		{
			if (stayedInZone != newStayedInZone)
			{
				stayedInZone = newStayedInZone;
				if (stayedInZone)
				{
					RoR2Application.onFixedUpdate += FixedUpdateTeleporterCharging;
					GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
					UpdateStayedInZone();
				}
				else
				{
					killCount = 0;
					GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeath;
					RoR2Application.onFixedUpdate -= FixedUpdateTeleporterCharging;
				}
			}
		}

		private void FixedUpdateTeleporterCharging()
		{
			UpdateStayedInZone();
		}

		private void UpdateStayedInZone()
		{
			if (stayedInZone)
			{
				TeleporterInteraction instance = TeleporterInteraction.instance;
				CharacterBody currentBody = GetCurrentBody();
				if ((!instance || !instance.holdoutZoneController.IsBodyInChargingRadius(currentBody)) && (teleporterStartChargingTime + gracePeriod).hasPassed)
				{
					SetStayedInZone(newStayedInZone: false);
				}
			}
		}

		private void OnCharacterDeath(DamageReport damageReport)
		{
			if (damageReport.victimBodyIndex == beetleQueenBodyIndex)
			{
				killCount++;
				if (killCount >= killRequirement)
				{
					Grant();
				}
			}
		}
	}

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("ToolbotBody");
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

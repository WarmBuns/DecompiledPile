namespace RoR2.Achievements.Captain;

[RegisterAchievement("CaptainSupplyDropFinale", "Skills.Captain.UtilityAlt1", "CompleteMainEnding", 3u, typeof(CaptainSupplyDropFinaleServerAchievement))]
public class CaptainSupplyDropFinaleAchievement : BaseAchievement
{
	private class CaptainSupplyDropFinaleServerAchievement : BaseServerAchievement
	{
		private const float lastHitWindowSeconds = 0.1f;

		private BodyIndex requiredVictimBodyIndex;

		private float lastHitTime;

		public override void OnInstall()
		{
			base.OnInstall();
			requiredVictimBodyIndex = BodyCatalog.FindBodyIndex("BrotherHurtBody");
			GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeathGlobal;
			GlobalEventManager.onServerDamageDealt += OnServerDamageDealt;
			lastHitTime = float.NegativeInfinity;
		}

		public override void OnUninstall()
		{
			GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeathGlobal;
			GlobalEventManager.onServerDamageDealt -= OnServerDamageDealt;
			base.OnUninstall();
		}

		private void OnServerDamageDealt(DamageReport damageReport)
		{
			if (damageReport.victimBodyIndex == requiredVictimBodyIndex && DoesDamageQualify(damageReport) && (object)serverAchievementTracker.networkUser.master == damageReport.attackerMaster && (bool)damageReport.attackerMaster)
			{
				lastHitTime = Run.FixedTimeStamp.now.t;
			}
		}

		private void OnCharacterDeathGlobal(DamageReport damageReport)
		{
			if (damageReport.victimBodyIndex == requiredVictimBodyIndex && (object)serverAchievementTracker.networkUser.master == damageReport.attackerMaster && (bool)damageReport.attackerMaster && (DoesDamageQualify(damageReport) || Run.FixedTimeStamp.now.t - lastHitTime <= 0.1f))
			{
				Grant();
			}
		}

		private bool DoesDamageQualify(DamageReport damageReport)
		{
			GenericDisplayNameProvider component = damageReport.damageInfo.inflictor.GetComponent<GenericDisplayNameProvider>();
			if ((bool)component && component.displayToken != null)
			{
				return component.displayToken.StartsWith("CAPTAIN_SUPPLY_");
			}
			return false;
		}
	}

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("CaptainBody");
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

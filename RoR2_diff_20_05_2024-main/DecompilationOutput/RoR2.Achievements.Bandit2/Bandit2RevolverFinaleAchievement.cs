using System.Collections.Generic;

namespace RoR2.Achievements.Bandit2;

[RegisterAchievement("Bandit2RevolverFinale", "Skills.Bandit2.SkullRevolver", "CompleteThreeStages", 3u, typeof(Bandit2RevolverFinaleServerAchievement))]
public class Bandit2RevolverFinaleAchievement : BaseAchievement
{
	private class Bandit2RevolverFinaleServerAchievement : BaseServerAchievement
	{
		private const float lastHitWindowSeconds = 0.1f;

		private List<BodyIndex> requiredVictimBodyIndices;

		private float lastHitTime;

		public override void OnInstall()
		{
			base.OnInstall();
			requiredVictimBodyIndices = new List<BodyIndex>();
			requiredVictimBodyIndices.Add(BodyCatalog.FindBodyIndex("BrotherHurtBody"));
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
			if (DoesDamageQualify(damageReport) && !(damageReport.attackerMaster == null) && (object)serverAchievementTracker.networkUser.master == damageReport.attackerMaster && requiredVictimBodyIndices.Contains(damageReport.victimBodyIndex))
			{
				lastHitTime = Run.FixedTimeStamp.now.t;
			}
		}

		private void OnCharacterDeathGlobal(DamageReport damageReport)
		{
			if ((DoesDamageQualify(damageReport) || !(Run.FixedTimeStamp.now.t - lastHitTime > 0.1f)) && !(damageReport.attackerMaster == null) && (object)serverAchievementTracker.networkUser.master == damageReport.attackerMaster && requiredVictimBodyIndices.Contains(damageReport.victimBodyIndex))
			{
				Grant();
			}
		}

		private bool DoesDamageQualify(DamageReport damageReport)
		{
			return (DamageType)(damageReport.damageInfo.damageType & DamageType.ResetCooldownsOnKill) == DamageType.ResetCooldownsOnKill;
		}
	}

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("Bandit2Body");
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

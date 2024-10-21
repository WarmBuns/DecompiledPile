using EntityStates.FalseSon;

namespace RoR2.Achievements.FalseSon;

[RegisterAchievement("FalseSonLaserMultiKill", "Skills.FalseSon.LaserBurst", "UnlockFalseSon", 3u, typeof(FalseSonLaserMultiKillServerAchievement))]
public class FalseSonLaserMultiKill : BaseAchievement
{
	private class FalseSonLaserMultiKillServerAchievement : BaseServerAchievement
	{
		private int killCount;

		private float localLaserDuration;

		public override void OnInstall()
		{
			base.OnInstall();
			GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
			LaserFatherCharged.laserTracking += TrackLaserActivation;
		}

		public override void OnUninstall()
		{
			LaserFatherCharged.laserTracking -= TrackLaserActivation;
			GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeath;
			base.OnUninstall();
		}

		private void TrackLaserActivation(float laserDuration)
		{
			localLaserDuration = laserDuration;
			if (localLaserDuration <= 0f)
			{
				killCount = 0;
			}
		}

		private void OnCharacterDeath(DamageReport damageReport)
		{
			if (localLaserDuration > 0f && (object)damageReport.attackerMaster != null && (object)damageReport.attackerMaster == base.networkUser.master && (bool)damageReport.victimBody && (damageReport.victimBody.bodyFlags & CharacterBody.BodyFlags.Masterless) == 0)
			{
				killCount++;
				if (requirement <= killCount)
				{
					Grant();
				}
			}
		}
	}

	private static readonly int requirement = 15;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("FalseSonBody");
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

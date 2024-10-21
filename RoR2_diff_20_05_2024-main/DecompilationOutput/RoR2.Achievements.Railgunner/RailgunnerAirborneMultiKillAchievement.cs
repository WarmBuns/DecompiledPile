using Assets.RoR2.Scripts.Platform;
using EntityStates.Railgunner.Weapon;

namespace RoR2.Achievements.Railgunner;

[RegisterAchievement("RailgunnerAirborneMultiKill", "Skills.Railgunner.SpecialAlt1", null, 3u, typeof(RailgunnerAirborneMultiKillServerAchievement))]
public class RailgunnerAirborneMultiKillAchievement : BaseAchievement
{
	private class RailgunnerAirborneMultiKillServerAchievement : BaseServerAchievement
	{
		private int killCount;

		public override void OnInstall()
		{
			base.OnInstall();
			GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
			BaseFireSnipe.onFireSnipe += OnFireSnipe;
		}

		private void OnFireSnipe(BaseFireSnipe state)
		{
			killCount = 0;
		}

		public override void OnUninstall()
		{
			GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeath;
			base.OnUninstall();
		}

		private bool CharacterIsInAir()
		{
			CharacterBody currentBody = base.networkUser.GetCurrentBody();
			if ((bool)currentBody && (bool)currentBody.characterMotor)
			{
				return !currentBody.characterMotor.isGrounded;
			}
			return false;
		}

		private void OnCharacterDeath(DamageReport damageReport)
		{
			if (damageReport.damageInfo.attacker == damageReport.damageInfo.inflictor && (object)damageReport.attackerMaster != null && (object)damageReport.attackerMaster == base.networkUser.master && CharacterIsInAir())
			{
				killCount++;
				if (requirement <= killCount)
				{
					Grant();
					ServerTryToCompleteActivity();
				}
			}
		}
	}

	private static readonly int requirement = 3;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("RailgunnerBody");
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

	public override void TryToCompleteActivity()
	{
		if (base.localUser.id == LocalUserManager.GetFirstLocalUser().id && shouldGrant)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "RailgunnerAirborneMultiKill";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}
}

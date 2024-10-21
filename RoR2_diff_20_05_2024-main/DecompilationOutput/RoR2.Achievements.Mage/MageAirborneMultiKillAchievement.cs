using Assets.RoR2.Scripts.Platform;

namespace RoR2.Achievements.Mage;

[RegisterAchievement("MageAirborneMultiKill", "Skills.Mage.FlyUp", "FreeMage", 3u, typeof(MageAirborneMultiKillServerAchievement))]
public class MageAirborneMultiKillAchievement : BaseAchievement
{
	private class MageAirborneMultiKillServerAchievement : BaseServerAchievement
	{
		private int killCount;

		public override void OnInstall()
		{
			base.OnInstall();
			RoR2Application.onFixedUpdate += OnFixedUpdate;
			GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
		}

		public override void OnUninstall()
		{
			GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeath;
			RoR2Application.onFixedUpdate -= OnFixedUpdate;
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

		private void OnFixedUpdate()
		{
			if (!CharacterIsInAir())
			{
				killCount = 0;
			}
		}

		private void OnCharacterDeath(DamageReport damageReport)
		{
			if ((object)damageReport.attackerMaster == base.networkUser.master && (object)damageReport.attackerMaster != null && CharacterIsInAir())
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

	private static readonly int requirement = 15;

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

	public override void TryToCompleteActivity()
	{
		if (base.localUser.id == LocalUserManager.GetFirstLocalUser().id && shouldGrant)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "MageAirborneMultiKill";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}
}

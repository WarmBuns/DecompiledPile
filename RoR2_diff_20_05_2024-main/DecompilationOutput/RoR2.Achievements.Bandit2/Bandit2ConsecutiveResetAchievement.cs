using Assets.RoR2.Scripts.Platform;
using RoR2.Skills;

namespace RoR2.Achievements.Bandit2;

[RegisterAchievement("Bandit2ConsecutiveReset", "Skills.Bandit2.Rifle", "CompleteThreeStages", 3u, typeof(Bandit2ConsecutiveResetServerAchievement))]
public class Bandit2ConsecutiveResetAchievement : BaseAchievement
{
	private class Bandit2ConsecutiveResetServerAchievement : BaseServerAchievement
	{
		private int progress;

		private SkillDef requiredSkillDef;

		private bool waitingForKill;

		private CharacterBody _trackedBody;

		private CharacterBody trackedBody
		{
			get
			{
				return _trackedBody;
			}
			set
			{
				if ((object)_trackedBody != value)
				{
					if ((object)_trackedBody != null)
					{
						_trackedBody.onSkillActivatedServer -= OnBodySKillActivatedServer;
					}
					_trackedBody = value;
					if ((object)_trackedBody != null)
					{
						_trackedBody.onSkillActivatedServer += OnBodySKillActivatedServer;
						progress = 0;
						waitingForKill = false;
					}
				}
			}
		}

		private void OnBodySKillActivatedServer(GenericSkill skillSlot)
		{
			if ((object)skillSlot.skillDef == requiredSkillDef && (object)requiredSkillDef != null)
			{
				if (waitingForKill)
				{
					progress = 0;
				}
				waitingForKill = true;
			}
		}

		public override void OnInstall()
		{
			base.OnInstall();
			requiredSkillDef = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("Bandit2.ResetRevolver"));
			GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeathGlobal;
			RoR2Application.onFixedUpdate += FixedUpdate;
		}

		public override void OnUninstall()
		{
			trackedBody = null;
			RoR2Application.onFixedUpdate -= FixedUpdate;
			GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeathGlobal;
			base.OnUninstall();
		}

		private void FixedUpdate()
		{
			trackedBody = GetCurrentBody();
		}

		private void OnCharacterDeathGlobal(DamageReport damageReport)
		{
			if ((DamageType)(damageReport.damageInfo.damageType & DamageType.ResetCooldownsOnKill) == DamageType.ResetCooldownsOnKill && !(damageReport.attackerBody == null) && (object)damageReport.attackerBody == trackedBody)
			{
				waitingForKill = false;
				progress++;
				if (progress >= requirement)
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

	public override void TryToCompleteActivity()
	{
		bool flag = base.localUser.id == LocalUserManager.GetFirstLocalUser().id;
		if (shouldGrant && flag)
		{
			BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
			baseActivitySelector.activityAchievementID = "Bandit2ConsecutiveReset";
			PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector);
		}
	}
}

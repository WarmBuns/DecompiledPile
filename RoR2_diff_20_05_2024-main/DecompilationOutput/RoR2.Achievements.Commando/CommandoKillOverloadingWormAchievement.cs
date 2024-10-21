namespace RoR2.Achievements.Commando;

[RegisterAchievement("CommandoKillOverloadingWorm", "Skills.Commando.FireShotgunBlast", null, 3u, typeof(CommandoKillOverloadingWormServerAchievement))]
public class CommandoKillOverloadingWormAchievement : BaseAchievement
{
	public class CommandoKillOverloadingWormServerAchievement : BaseServerAchievement
	{
		private BodyIndex overloadingWormBodyIndex;

		public override void OnInstall()
		{
			base.OnInstall();
			overloadingWormBodyIndex = BodyCatalog.FindBodyIndex("ElectricWormBody");
			GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
		}

		public override void OnUninstall()
		{
			GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeath;
			base.OnUninstall();
		}

		private void OnCharacterDeath(DamageReport damageReport)
		{
			if ((bool)damageReport.victimBody && damageReport.victimBody.bodyIndex == overloadingWormBodyIndex && IsCurrentBody(damageReport.damageInfo.attacker))
			{
				Grant();
			}
		}
	}

	private int commandoBodyIndex;

	protected override BodyIndex LookUpRequiredBodyIndex()
	{
		return BodyCatalog.FindBodyIndex("CommandoBody");
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

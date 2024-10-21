using EntityStates.Destructible;

namespace RoR2.Achievements;

[RegisterAchievement("FindDevilAltar", "Items.NovaOnHeal", null, 3u, null)]
public class FindDevilAltarAchievement : BaseAchievement
{
	public override void OnInstall()
	{
		base.OnInstall();
		AltarSkeletonDeath.onDeath += OnDeath;
	}

	public override void OnUninstall()
	{
		base.OnUninstall();
		AltarSkeletonDeath.onDeath -= OnDeath;
	}

	private void OnDeath()
	{
		Grant();
	}
}

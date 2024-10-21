namespace RoR2.Achievements;

[RegisterAchievement("AttackSpeed", "Items.AttackSpeedOnCrit", null, 2u, null)]
public class AttackSpeedAchievement : BaseAchievement
{
	private const float requirement = 3f;

	public override void OnInstall()
	{
		base.OnInstall();
		RoR2Application.onUpdate += CheckAttackSpeed;
	}

	public override void OnUninstall()
	{
		RoR2Application.onUpdate -= CheckAttackSpeed;
		base.OnUninstall();
	}

	public void CheckAttackSpeed()
	{
		if (base.localUser != null && (bool)base.localUser.cachedBody && base.localUser.cachedBody.attackSpeed >= 3f)
		{
			Grant();
		}
	}
}

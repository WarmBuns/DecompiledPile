namespace RoR2.Achievements;

[RegisterAchievement("MoveSpeed", "Items.JumpBoost", null, 2u, null)]
public class MoveSpeedAchievement : BaseAchievement
{
	private const float requirement = 4f;

	public override void OnInstall()
	{
		base.OnInstall();
		RoR2Application.onUpdate += CheckMoveSpeed;
	}

	public override void OnUninstall()
	{
		RoR2Application.onUpdate -= CheckMoveSpeed;
		base.OnUninstall();
	}

	public void CheckMoveSpeed()
	{
		if (base.localUser != null && (bool)base.localUser.cachedBody && base.localUser.cachedBody.moveSpeed / base.localUser.cachedBody.baseMoveSpeed >= 4f)
		{
			Grant();
		}
	}
}

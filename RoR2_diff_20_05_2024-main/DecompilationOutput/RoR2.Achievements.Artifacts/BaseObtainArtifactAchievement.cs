namespace RoR2.Achievements.Artifacts;

public abstract class BaseObtainArtifactAchievement : BaseAchievement
{
	private UnlockableDef unlockableDef;

	protected abstract ArtifactDef artifactDef { get; }

	public override void OnInstall()
	{
		base.OnInstall();
		unlockableDef = artifactDef.unlockableDef;
		UserProfile.onUnlockableGranted += OnUnlockableGranted;
		if ((object)unlockableDef != null && base.userProfile.HasUnlockable(unlockableDef))
		{
			Grant();
		}
	}

	public override void OnUninstall()
	{
		UserProfile.onUnlockableGranted -= OnUnlockableGranted;
		unlockableDef = null;
		base.OnUninstall();
	}

	private void OnUnlockableGranted(UserProfile userProfile, UnlockableDef unlockableDef)
	{
		if ((object)unlockableDef == this.unlockableDef)
		{
			Grant();
		}
	}
}

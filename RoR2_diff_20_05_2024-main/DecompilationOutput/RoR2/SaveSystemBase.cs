using Zio;

namespace RoR2;

public abstract class SaveSystemBase
{
	protected abstract void ProcessFileOutputQueue();

	protected abstract void StartSave(UserProfile userProfile, bool blocking);

	protected abstract LoadUserProfileOperationResult LoadUserProfileFromDisk(IFileSystem fileSystem, UPath path);

	public abstract void LoadUserProfiles();

	public abstract UserProfile LoadPrimaryProfile();

	public abstract string GetPlatformUsernameOrDefault(string defaultName);

	public abstract void LoadInitialData();
}

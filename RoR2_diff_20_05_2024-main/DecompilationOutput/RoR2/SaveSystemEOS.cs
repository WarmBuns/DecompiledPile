namespace RoR2;

public class SaveSystemEOS : SaveSystemSteam
{
	public override string GetPlatformUsernameOrDefault(string defaultName)
	{
		return PlatformSystems.userManager.GetUserName();
	}
}

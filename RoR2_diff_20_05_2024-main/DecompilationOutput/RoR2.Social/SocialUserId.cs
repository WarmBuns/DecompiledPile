namespace RoR2.Social;

public struct SocialUserId
{
	public readonly PlatformID steamId;

	public SocialUserId(PlatformID steamId)
	{
		this.steamId = steamId;
	}
}

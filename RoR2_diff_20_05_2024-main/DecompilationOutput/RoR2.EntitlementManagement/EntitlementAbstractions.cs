using Facepunch.Steamworks;
using RoR2.Networking;

namespace RoR2.EntitlementManagement;

public static class EntitlementAbstractions
{
	public enum LoggedInPlatform
	{
		NONE_ERROR,
		STEAMWORKS,
		EPIC_ONLINE_SERVICES
	}

	public static LoggedInPlatform loggedInPlatform;

	public static bool VerifyLocalSteamUser(EntitlementDef entitlementDef)
	{
		return Client.Instance.App.IsDlcInstalled(entitlementDef.steamAppId);
	}

	public static bool VerifyRemoteUser(ClientAuthData authData, EntitlementDef entitlementDef)
	{
		for (int i = 0; i < authData.entitlements.Length; i++)
		{
			if (authData.entitlements[i].Equals(entitlementDef.name))
			{
				return true;
			}
		}
		return false;
	}

	public static void OpenShopPage(EntitlementDef entitlementDef)
	{
	}
}

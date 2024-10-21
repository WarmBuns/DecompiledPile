using Epic.OnlineServices;
using Epic.OnlineServices.Connect;

namespace RoR2;

public static class EOSExtensions
{
	public static ProductUserId ToProductUserId(this EpicAccountId epicAccountId)
	{
		GetExternalAccountMappingsOptions getExternalAccountMappingsOptions = default(GetExternalAccountMappingsOptions);
		getExternalAccountMappingsOptions.AccountIdType = ExternalAccountType.Epic;
		getExternalAccountMappingsOptions.LocalUserId = EOSLoginManager.loggedInProductId;
		getExternalAccountMappingsOptions.TargetExternalUserId = epicAccountId.ToString();
		GetExternalAccountMappingsOptions options = getExternalAccountMappingsOptions;
		return EOSPlatformManager.GetPlatformInterface().GetConnectInterface().GetExternalAccountMapping(ref options);
	}
}

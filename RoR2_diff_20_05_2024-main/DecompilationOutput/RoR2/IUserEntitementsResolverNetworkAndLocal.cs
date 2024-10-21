using RoR2.EntitlementManagement;

namespace RoR2;

public interface IUserEntitementsResolverNetworkAndLocal : IUserEntitlementResolver<NetworkUser>, IUserEntitlementResolver<LocalUser>, IPlatformShopLauncher
{
	string[] BuildEntitlements();
}

using System;

namespace RoR2.EntitlementManagement;

public interface IPlatformShopLauncher
{
	event Action OnPlatformShopClosed;

	void LaunchPlatformShop(EntitlementDef entitlementDef);

	void LaunchPlatformShopAllProducts();
}

using System;
using System.Collections.Generic;
using Facepunch.Steamworks;
using JetBrains.Annotations;
using RoR2.ContentManagement;
using RoR2.Networking;

namespace RoR2.EntitlementManagement;

public class SteamworksEntitlementResolver : IUserEntitementsResolverNetworkAndLocal, IUserEntitlementResolver<NetworkUser>, IUserEntitlementResolver<LocalUser>, IPlatformShopLauncher
{
	private static event Action onDlcInstalled;

	public event Action OnPlatformShopClosed;

	event Action IUserEntitlementResolver<LocalUser>.onEntitlementsChanged
	{
		add
		{
			onDlcInstalled += value;
		}
		remove
		{
			onDlcInstalled -= value;
		}
	}

	event Action IUserEntitlementResolver<NetworkUser>.onEntitlementsChanged
	{
		add
		{
		}
		remove
		{
		}
	}

	private static void OnDlcInstalled(uint appId)
	{
		SteamworksEntitlementResolver.onDlcInstalled?.Invoke();
	}

	bool IUserEntitlementResolver<LocalUser>.CheckUserHasEntitlement([NotNull] LocalUser localUser, [NotNull] EntitlementDef entitlementDef)
	{
		return EntitlementAbstractions.VerifyLocalSteamUser(entitlementDef);
	}

	bool IUserEntitlementResolver<NetworkUser>.CheckUserHasEntitlement([NotNull] NetworkUser networkUser, [NotNull] EntitlementDef entitlementDef)
	{
		if (!networkUser)
		{
			return false;
		}
		if (networkUser.isLocalPlayer)
		{
			return EntitlementAbstractions.VerifyLocalSteamUser(entitlementDef);
		}
		ClientAuthData clientAuthData = ServerAuthManager.FindAuthData(networkUser.connectionToClient);
		if (clientAuthData == null)
		{
			return false;
		}
		PlatformID platformId = clientAuthData.platformId;
		if (!platformId.isValid)
		{
			return false;
		}
		return EntitlementAbstractions.VerifyRemoteUser(clientAuthData, entitlementDef);
	}

	public SteamworksEntitlementResolver()
	{
		EntitlementManager.collectLocalUserEntitlementResolvers += delegate(Action<IUserEntitlementResolver<LocalUser>> add)
		{
			add(this);
		};
		EntitlementManager.collectNetworkUserEntitlementResolvers += delegate(Action<IUserEntitlementResolver<NetworkUser>> add)
		{
			add(this);
		};
		SteamworksClientManager.onLoaded += delegate
		{
			SteamworksClientManager.instance.steamworksClient.App.OnDlcInstalled += OnDlcInstalled;
		};
	}

	public string[] BuildEntitlements()
	{
		List<string> list = new List<string>();
		EntitlementDef[] entitlementDefs = ContentManager.entitlementDefs;
		foreach (EntitlementDef entitlementDef in entitlementDefs)
		{
			if (Client.Instance.App.IsDlcInstalled(entitlementDef.steamAppId))
			{
				list.Add(entitlementDef.name);
			}
		}
		return list.ToArray();
	}

	public void LaunchPlatformShop(EntitlementDef entitlementDef)
	{
		throw new NotImplementedException();
	}

	public void LaunchPlatformShopAllProducts()
	{
		throw new NotImplementedException();
	}
}

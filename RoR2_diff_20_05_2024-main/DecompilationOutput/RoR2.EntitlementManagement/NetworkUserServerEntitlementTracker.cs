using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace RoR2.EntitlementManagement;

public class NetworkUserServerEntitlementTracker : BaseUserEntitlementTracker<NetworkUser>, IDisposable
{
	protected override void SubscribeToUserDiscovered()
	{
		NetworkUser.onNetworkUserDiscovered += OnUserDiscovered;
	}

	protected override void SubscribeToUserLost()
	{
		NetworkUser.onNetworkUserLost += OnUserLost;
	}

	protected override void UnsubscribeFromUserDiscovered()
	{
		NetworkUser.onNetworkUserDiscovered -= OnUserDiscovered;
	}

	protected override void UnsubscribeFromUserLost()
	{
		NetworkUser.onNetworkUserLost -= OnUserLost;
	}

	protected override IList<NetworkUser> GetCurrentUsers()
	{
		return NetworkUser.readOnlyInstancesList;
	}

	public NetworkUserServerEntitlementTracker(IUserEntitlementResolver<NetworkUser>[] entitlementResolvers)
		: base(entitlementResolvers)
	{
		NetworkUser.onPostNetworkUserStart += OnPostNetworkUserStart;
	}

	private void OnPostNetworkUserStart(NetworkUser networkUser)
	{
		if (NetworkServer.active)
		{
			UpdateUserEntitlements(networkUser);
		}
	}
}

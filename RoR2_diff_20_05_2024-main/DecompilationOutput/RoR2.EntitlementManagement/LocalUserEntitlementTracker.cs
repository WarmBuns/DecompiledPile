using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace RoR2.EntitlementManagement;

public class LocalUserEntitlementTracker : BaseUserEntitlementTracker<LocalUser>, IDisposable
{
	protected override void SubscribeToUserDiscovered()
	{
		LocalUserManager.onUserSignIn += OnUserDiscovered;
	}

	protected override void SubscribeToUserLost()
	{
		LocalUserManager.onUserSignOut += OnUserLost;
	}

	protected override void UnsubscribeFromUserDiscovered()
	{
		LocalUserManager.onUserSignIn -= OnUserDiscovered;
	}

	protected override void UnsubscribeFromUserLost()
	{
		LocalUserManager.onUserSignOut -= OnUserLost;
	}

	protected override IList<LocalUser> GetCurrentUsers()
	{
		return LocalUserManager.readOnlyLocalUsersList;
	}

	public LocalUserEntitlementTracker([NotNull] IUserEntitlementResolver<LocalUser>[] entitlementResolvers)
		: base(entitlementResolvers)
	{
	}
}

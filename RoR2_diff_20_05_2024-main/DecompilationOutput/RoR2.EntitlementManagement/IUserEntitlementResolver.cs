using System;
using JetBrains.Annotations;

namespace RoR2.EntitlementManagement;

public interface IUserEntitlementResolver<TUser> where TUser : class
{
	event Action onEntitlementsChanged;

	bool CheckUserHasEntitlement([NotNull] TUser user, [NotNull] EntitlementDef entitlementDef);
}

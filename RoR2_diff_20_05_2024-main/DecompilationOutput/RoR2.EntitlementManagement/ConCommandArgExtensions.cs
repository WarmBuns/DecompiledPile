using System;

namespace RoR2.EntitlementManagement;

public static class ConCommandArgExtensions
{
	public static EntitlementIndex? TryGetArgEntitlementIndex(this ConCommandArgs args, int index)
	{
		string text = args.TryGetArgString(index);
		if (text != null)
		{
			EntitlementIndex entitlementIndex = EntitlementCatalog.FindEntitlementIndex(text);
			if (entitlementIndex != EntitlementIndex.None || text.Equals("None", StringComparison.Ordinal))
			{
				return entitlementIndex;
			}
		}
		return null;
	}

	public static EntitlementIndex GetArgEntitlementIndex(this ConCommandArgs args, int index)
	{
		return args.TryGetArgEntitlementIndex(index) ?? throw new ConCommandException("No EntitlementIndex is defined for an entitlement named '" + args.TryGetArgString(index) + "'. Use the \"entitlement_list\" command to get a list of all valid entitlements.");
	}
}

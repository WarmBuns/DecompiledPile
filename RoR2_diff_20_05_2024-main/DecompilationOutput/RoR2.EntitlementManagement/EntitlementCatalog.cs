using System;
using System.Collections.Generic;
using System.Text;
using HG;
using JetBrains.Annotations;
using RoR2.ContentManagement;

namespace RoR2.EntitlementManagement;

public static class EntitlementCatalog
{
	private static EntitlementDef[] _entitlementDefs = Array.Empty<EntitlementDef>();

	private static Dictionary<string, EntitlementIndex> nameToIndex = new Dictionary<string, EntitlementIndex>();

	private static string[] indexToName = Array.Empty<string>();

	public static ReadOnlyArray<EntitlementDef> entitlementDefs => _entitlementDefs;

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		SetEntitlementDefs(ContentManager.entitlementDefs);
	}

	private static void SetEntitlementDefs(EntitlementDef[] newEntitlementDefs)
	{
		_entitlementDefs = ArrayUtils.Clone(newEntitlementDefs);
		nameToIndex.Clear();
		Array.Resize(ref indexToName, newEntitlementDefs.Length);
		for (int i = 0; i < entitlementDefs.Length; i++)
		{
			_entitlementDefs[i].entitlementIndex = (EntitlementIndex)i;
			string name = _entitlementDefs[i].name;
			nameToIndex[name] = (EntitlementIndex)i;
			indexToName[i] = name;
		}
	}

	public static EntitlementIndex FindEntitlementIndex([NotNull] string entitlementName)
	{
		if (nameToIndex.TryGetValue(entitlementName, out var value))
		{
			return value;
		}
		return EntitlementIndex.None;
	}

	public static EntitlementDef GetEntitlementDef(EntitlementIndex entitlementIndex)
	{
		return ArrayUtils.GetSafe(_entitlementDefs, (int)entitlementIndex);
	}

	[ConCommand(commandName = "entitlements_list", flags = ConVarFlags.None, helpText = "Displays all registered entitlements.")]
	private static void CCEntitlementsList(ConCommandArgs args)
	{
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		for (int i = 0; i < indexToName.Length; i++)
		{
			stringBuilder.AppendLine(indexToName[i]);
		}
		args.Log(stringBuilder.ToString());
		HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
	}
}

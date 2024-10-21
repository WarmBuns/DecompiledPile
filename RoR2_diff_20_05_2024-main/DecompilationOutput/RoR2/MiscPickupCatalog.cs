using System;
using System.Collections.Generic;
using HG;
using RoR2.ContentManagement;

namespace RoR2;

public static class MiscPickupCatalog
{
	private static MiscPickupDef[] _miscPickupDefs = Array.Empty<MiscPickupDef>();

	public static ResourceAvailability availability = default(ResourceAvailability);

	public static IReadOnlyList<MiscPickupDef> miscPickupDefs => _miscPickupDefs;

	public static int pickupCount => _miscPickupDefs.Length;

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		ArrayUtils.CloneTo(ContentManager.miscPickupDefs, ref _miscPickupDefs);
		for (int i = 0; i < _miscPickupDefs.Length; i++)
		{
			_miscPickupDefs[i].miscPickupIndex = (MiscPickupIndex)i;
		}
		availability.MakeAvailable();
	}

	public static MiscPickupDef GetMiscDef(MiscPickupIndex miscIndex)
	{
		return ArrayUtils.GetSafe(_miscPickupDefs, (int)miscIndex);
	}
}

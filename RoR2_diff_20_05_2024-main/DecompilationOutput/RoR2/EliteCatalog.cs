using System;
using System.Collections.Generic;
using HG;
using RoR2.ContentManagement;
using RoR2.Modding;

namespace RoR2;

public static class EliteCatalog
{
	public static List<EliteIndex> eliteList = new List<EliteIndex>();

	private static EliteDef[] eliteDefs;

	[Obsolete("Use IContentPackProvider instead.")]
	public static readonly CatalogModHelperProxy<EliteDef> modHelper = new CatalogModHelperProxy<EliteDef>("RoR2.EliteCatalog.modHelper", LegacyModContentPackProvider.instance.registrationContentPack.eliteDefs);

	[SystemInitializer(new Type[] { })]
	private static void Init()
	{
		SetEliteDefs(ContentManager.eliteDefs);
	}

	private static void SetEliteDefs(EliteDef[] newEliteDefs)
	{
		eliteDefs = ArrayUtils.Clone(newEliteDefs);
		for (EliteIndex eliteIndex = (EliteIndex)0; (int)eliteIndex < eliteDefs.Length; eliteIndex++)
		{
			RegisterElite(eliteIndex, eliteDefs[(int)eliteIndex]);
		}
	}

	private static void RegisterElite(EliteIndex eliteIndex, EliteDef eliteDef)
	{
		eliteDef.eliteIndex = eliteIndex;
		eliteList.Add(eliteIndex);
		eliteDefs[(int)eliteIndex] = eliteDef;
	}

	public static EliteDef GetEliteDef(EliteIndex eliteIndex)
	{
		return ArrayUtils.GetSafe(eliteDefs, (int)eliteIndex);
	}
}

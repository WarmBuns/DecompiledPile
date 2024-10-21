using System;
using System.Collections.Generic;
using RoR2.ContentManagement;
using RoR2.Modding;

namespace RoR2;

public class CatalogModHelperProxy<TEntry>
{
	private string name;

	private NamedAssetCollection<TEntry> dest;

	[Obsolete("Use IContentPackProvider instead.")]
	public event Action<List<TEntry>> getAdditionalEntries
	{
		add
		{
			LegacyModContentPackProvider.instance.HandleLegacyGetAdditionalEntries(name, value, dest);
		}
		remove
		{
		}
	}

	public CatalogModHelperProxy(string name, NamedAssetCollection<TEntry> dest)
	{
		this.name = name;
		this.dest = dest;
	}
}

using System;
using System.Collections.Generic;

namespace RoR2;

public class CatalogModHelper<TEntry>
{
	private readonly Action<int, TEntry> registrationDelegate;

	private readonly Func<TEntry, string> nameGetter;

	public event Action<List<TEntry>> getAdditionalEntries;

	public CatalogModHelper(Action<int, TEntry> registrationDelegate, Func<TEntry, string> nameGetter)
	{
		this.registrationDelegate = registrationDelegate;
		this.nameGetter = nameGetter;
	}

	public void CollectAndRegisterAdditionalEntries(ref TEntry[] entries)
	{
		int num = entries.Length;
		List<TEntry> list = new List<TEntry>();
		this.getAdditionalEntries?.Invoke(list);
		list.Sort((TEntry a, TEntry b) => StringComparer.Ordinal.Compare(nameGetter(a), nameGetter(b)));
		Array.Resize(ref entries, entries.Length + list.Count);
		int i = 0;
		for (int count = list.Count; i < count; i++)
		{
			entries[num + i] = list[i];
			registrationDelegate?.Invoke(num + i, list[i]);
		}
	}
}

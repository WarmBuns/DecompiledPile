using System;
using System.Collections.Generic;
using HG;

public struct KeyValueUnsplitter
{
	public readonly string baseKey;

	public KeyValueUnsplitter(string baseKey)
	{
		this.baseKey = baseKey;
	}

	public string GetValue(IEnumerable<KeyValuePair<string, string>> keyValues)
	{
		List<KeyValuePair<int, string>> list = CollectionPool<KeyValuePair<int, string>, List<KeyValuePair<int, string>>>.RentCollection();
		foreach (KeyValuePair<string, string> keyValue in keyValues)
		{
			if (keyValue.Key.EndsWith("]", StringComparison.Ordinal))
			{
				int num = keyValue.Key.LastIndexOf("[", StringComparison.Ordinal);
				string value = keyValue.Key.Substring(0, num);
				if (baseKey.Equals(value, StringComparison.Ordinal) && int.TryParse(keyValue.Key.Substring(num + 1), out var result))
				{
					list.Add(new KeyValuePair<int, string>(result, keyValue.Value));
				}
			}
			else if (baseKey.Equals(keyValue.Key, StringComparison.Ordinal))
			{
				return keyValue.Value;
			}
		}
		list.Sort((KeyValuePair<int, string> a, KeyValuePair<int, string> b) => a.Key.CompareTo(b.Key));
		string result2 = string.Concat(list);
		CollectionPool<KeyValuePair<int, string>, List<KeyValuePair<int, string>>>.ReturnCollection(list);
		return result2;
	}
}

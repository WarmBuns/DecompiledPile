using System;
using System.Collections.Generic;
using System.Linq;

namespace RoR2.Orbs;

public static class OrbCatalog
{
	private static readonly Dictionary<Type, int> typeToIndex;

	private static Type[] indexToType;

	private static void GenerateCatalog()
	{
		indexToType = (from t in typeof(Orb).Assembly.GetTypes()
			where t.IsSubclassOf(typeof(Orb))
			orderby t.Name
			select t).ToArray();
		typeToIndex.Clear();
		Type[] array = indexToType;
		foreach (Type key in array)
		{
			typeToIndex[key] = typeToIndex.Count;
		}
	}

	static OrbCatalog()
	{
		typeToIndex = new Dictionary<Type, int>();
		indexToType = Array.Empty<Type>();
		GenerateCatalog();
	}

	public static int FindIndex(Type type)
	{
		if (typeToIndex.TryGetValue(type, out var value))
		{
			return value;
		}
		return -1;
	}

	public static Type FindType(int index)
	{
		if (index < 0 || index >= indexToType.Length)
		{
			return null;
		}
		return indexToType[index];
	}

	public static Orb Instantiate(int index)
	{
		return Instantiate(FindType(index));
	}

	public static Orb Instantiate(Type type)
	{
		if (type == null)
		{
			return null;
		}
		return (Orb)Activator.CreateInstance(type);
	}
}

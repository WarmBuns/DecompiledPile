using System.Collections.Generic;
using UnityEngine;

namespace RoR2.Orbs;

public static class OrbStorageUtility
{
	private static Dictionary<string, GameObject> _orbDictionary = new Dictionary<string, GameObject>();

	public static GameObject Get(string path)
	{
		if (!_orbDictionary.ContainsKey(path))
		{
			_orbDictionary.Add(path, LegacyResourcesAPI.Load<GameObject>(path));
		}
		return _orbDictionary[path];
	}

	public static void Clear()
	{
		_orbDictionary.Clear();
	}
}

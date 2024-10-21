using UnityEngine;

public static class GlobalPrefabFunctions
{
	private static bool doSave = true;

	private static Transform FindRecursive(Transform t, string name)
	{
		if (t.name == name)
		{
			return t;
		}
		for (int i = 0; i < t.childCount; i++)
		{
			Transform child = t.GetChild(i);
			child = FindRecursive(child, name);
			if (child != null)
			{
				return child;
			}
		}
		return null;
	}
}

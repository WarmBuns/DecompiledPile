using UnityEngine;

public static class SingletonHelper
{
	public static void Assign<T>(ref T field, T instance) where T : Object
	{
		if (!field)
		{
			field = instance;
			return;
		}
		Debug.LogErrorFormat(instance, "Duplicate instance of singleton class {0}. Only one should exist at a time.", typeof(T).Name);
	}

	public static void Unassign<T>(ref T field, T instance) where T : Object
	{
		if (field == instance)
		{
			field = null;
		}
	}

	public static T Assign<T>(T existingInstance, T instance) where T : Object
	{
		if (!existingInstance)
		{
			return instance;
		}
		Debug.LogErrorFormat(instance, "Duplicate instance of singleton class {0}. Only one should exist at a time.", typeof(T).Name);
		return existingInstance;
	}

	public static T Unassign<T>(T existingInstance, T instance) where T : Object
	{
		if (instance == existingInstance)
		{
			return null;
		}
		return existingInstance;
	}
}

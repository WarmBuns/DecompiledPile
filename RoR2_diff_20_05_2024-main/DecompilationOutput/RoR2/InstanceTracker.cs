using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace RoR2;

public static class InstanceTracker
{
	private static class TypeData<T> where T : MonoBehaviour
	{
		public static readonly List<T> instancesList;

		static TypeData()
		{
			instancesList = new List<T>();
			instancesLists[typeof(T)] = instancesList;
		}

		public static void Add(T instance)
		{
			instancesList.Add(instance);
		}

		public static void Remove(T instance)
		{
			instancesList.Remove(instance);
		}
	}

	private static readonly Dictionary<Type, IEnumerable<MonoBehaviour>> instancesLists = new Dictionary<Type, IEnumerable<MonoBehaviour>>();

	public static void Add<T>([NotNull] T instance) where T : MonoBehaviour
	{
		TypeData<T>.Add(instance);
	}

	public static void Remove<T>([NotNull] T instance) where T : MonoBehaviour
	{
		TypeData<T>.Remove(instance);
	}

	[NotNull]
	public static List<T> GetInstancesList<T>() where T : MonoBehaviour
	{
		return TypeData<T>.instancesList;
	}

	public static T FirstOrNull<T>() where T : MonoBehaviour
	{
		if (TypeData<T>.instancesList.Count == 0)
		{
			return null;
		}
		return TypeData<T>.instancesList[0];
	}

	public static bool Any<T>() where T : MonoBehaviour
	{
		return TypeData<T>.instancesList.Count != 0;
	}

	[NotNull]
	public static IEnumerable<MonoBehaviour> FindInstancesEnumerable([NotNull] Type t)
	{
		if (!instancesLists.TryGetValue(t, out var value))
		{
			return Enumerable.Empty<MonoBehaviour>();
		}
		return value;
	}
}

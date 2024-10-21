using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public static class GetComponentsCache<T>
{
	private static readonly Stack<List<T>> buffers = new Stack<List<T>>();

	public static void ReturnBuffer(List<T> buffer)
	{
		buffer.Clear();
		buffers.Push(buffer);
	}

	private static List<T> RequestBuffer()
	{
		if (buffers.Count == 0)
		{
			return new List<T>();
		}
		return buffers.Pop();
	}

	public static List<T> GetGameObjectComponents(GameObject gameObject)
	{
		List<T> list = RequestBuffer();
		gameObject.GetComponents(list);
		return list;
	}

	public static List<T> GetGameObjectComponentsInChildren(GameObject gameObject, bool includeInactive = false)
	{
		List<T> list = RequestBuffer();
		gameObject.GetComponentsInChildren(includeInactive, list);
		return list;
	}
}

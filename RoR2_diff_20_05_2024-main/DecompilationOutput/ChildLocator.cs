using System;
using UnityEngine;

[DisallowMultipleComponent]
public class ChildLocator : MonoBehaviour
{
	[Serializable]
	private struct NameTransformPair
	{
		public string name;

		public Transform transform;
	}

	[SerializeField]
	private NameTransformPair[] transformPairs = Array.Empty<NameTransformPair>();

	public int Count => transformPairs.Length;

	public int FindChildIndex(string childName)
	{
		for (int i = 0; i < transformPairs.Length; i++)
		{
			if (childName == transformPairs[i].name)
			{
				return i;
			}
		}
		return -1;
	}

	public int FindChildIndex(Transform childTransform)
	{
		for (int i = 0; i < transformPairs.Length; i++)
		{
			if ((object)childTransform == transformPairs[i].transform)
			{
				return i;
			}
		}
		return -1;
	}

	public string FindChildName(int childIndex)
	{
		if ((uint)childIndex < transformPairs.Length)
		{
			return transformPairs[childIndex].name;
		}
		return null;
	}

	public Transform FindChild(string childName)
	{
		return FindChild(FindChildIndex(childName));
	}

	public GameObject FindChildGameObject(int childIndex)
	{
		Transform transform = FindChild(childIndex);
		if (!transform)
		{
			return null;
		}
		return transform.gameObject;
	}

	public GameObject FindChildGameObject(string childName)
	{
		return FindChildGameObject(FindChildIndex(childName));
	}

	public Transform FindChild(int childIndex)
	{
		if ((uint)childIndex < transformPairs.Length)
		{
			return transformPairs[childIndex].transform;
		}
		return null;
	}

	public T FindChildComponent<T>(string childName)
	{
		return FindChildComponent<T>(FindChildIndex(childName));
	}

	public T FindChildComponent<T>(int childIndex)
	{
		Transform transform = FindChild(childIndex);
		if (!transform)
		{
			return default(T);
		}
		return transform.GetComponent<T>();
	}
}

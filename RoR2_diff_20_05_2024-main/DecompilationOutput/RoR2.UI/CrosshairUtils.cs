using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoR2.UI;

public static class CrosshairUtils
{
	public enum OverridePriority
	{
		Sprint,
		Skill,
		PrioritySkill
	}

	public class OverrideRequest : IDisposable, IComparable<OverrideRequest>
	{
		public readonly GameObject prefab;

		public readonly OverridePriority priority;

		private Action<OverrideRequest> disposeCallback;

		public OverrideRequest(GameObject crosshairPrefab, OverridePriority overridePriority, Action<OverrideRequest> onDispose)
		{
			disposeCallback = onDispose;
			prefab = crosshairPrefab;
			priority = overridePriority;
		}

		public int CompareTo(OverrideRequest other)
		{
			return priority.CompareTo(other.priority);
		}

		public void Dispose()
		{
			disposeCallback?.Invoke(this);
			disposeCallback = null;
		}
	}

	private class CrosshairOverrideBehavior : MonoBehaviour
	{
		private List<OverrideRequest> requestList = new List<OverrideRequest>();

		public GameObject GetOverridePrefab()
		{
			if (requestList.Count > 0)
			{
				return requestList[requestList.Count - 1].prefab;
			}
			return null;
		}

		public OverrideRequest AddRequest(GameObject crosshairPrefab, OverridePriority overridePriority)
		{
			OverrideRequest overrideRequest = new OverrideRequest(crosshairPrefab, overridePriority, RemoveRequest);
			requestList.Add(overrideRequest);
			if (requestList.Count > 1)
			{
				requestList.Sort();
			}
			return overrideRequest;
		}

		private void RemoveRequest(OverrideRequest request)
		{
			requestList.Remove(request);
		}

		public void OnDestroy()
		{
			foreach (OverrideRequest request in requestList)
			{
				request.Dispose();
			}
			requestList.Clear();
		}
	}

	public static OverrideRequest RequestOverrideForBody(CharacterBody body, GameObject crosshairPrefab, OverridePriority overridePriority)
	{
		CrosshairOverrideBehavior crosshairOverrideBehavior = body.GetComponent<CrosshairOverrideBehavior>();
		if (!crosshairOverrideBehavior)
		{
			crosshairOverrideBehavior = body.gameObject.AddComponent<CrosshairOverrideBehavior>();
		}
		return crosshairOverrideBehavior.AddRequest(crosshairPrefab, overridePriority);
	}

	public static GameObject GetCrosshairPrefabForBody(CharacterBody body)
	{
		CrosshairOverrideBehavior component = body.GetComponent<CrosshairOverrideBehavior>();
		if ((bool)component)
		{
			GameObject overridePrefab = component.GetOverridePrefab();
			if ((bool)overridePrefab)
			{
				return overridePrefab;
			}
		}
		return body.defaultCrosshairPrefab;
	}
}

using System;
using UnityEngine;

namespace RoR2;

public class OnDestroyCallback : MonoBehaviour
{
	private Action<OnDestroyCallback> callback;

	public void OnDestroy()
	{
		if (callback != null)
		{
			callback(this);
		}
	}

	public static OnDestroyCallback AddCallback(GameObject gameObject, Action<OnDestroyCallback> callback)
	{
		OnDestroyCallback onDestroyCallback = gameObject.AddComponent<OnDestroyCallback>();
		onDestroyCallback.callback = callback;
		return onDestroyCallback;
	}

	public static void RemoveCallback(OnDestroyCallback callbackComponent)
	{
		callbackComponent.callback = null;
		UnityEngine.Object.Destroy(callbackComponent);
	}
}

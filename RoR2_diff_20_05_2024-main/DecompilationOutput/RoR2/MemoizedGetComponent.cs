using UnityEngine;

namespace RoR2;

public struct MemoizedGetComponent<TComponent> where TComponent : Component
{
	private GameObject cachedGameObject;

	private TComponent cachedValue;

	public TComponent Get(GameObject gameObject)
	{
		if (cachedGameObject != gameObject)
		{
			cachedGameObject = gameObject;
			cachedValue = (gameObject ? gameObject.GetComponent<TComponent>() : null);
		}
		return cachedValue;
	}
}

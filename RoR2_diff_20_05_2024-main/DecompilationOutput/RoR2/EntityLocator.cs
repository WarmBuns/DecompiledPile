using UnityEngine;

namespace RoR2;

[DisallowMultipleComponent]
public class EntityLocator : MonoBehaviour
{
	[Tooltip("The root gameobject of the entity.")]
	public GameObject entity;

	[SerializeField]
	private int entityZoneIndex;

	public int EntityZoneIndex => entityZoneIndex;

	public static bool HasEntityLocator(GameObject gameObject, out EntityLocator foundLocator)
	{
		if (gameObject == null)
		{
			foundLocator = null;
			return false;
		}
		foundLocator = gameObject.GetComponent<EntityLocator>();
		if (!foundLocator)
		{
			return false;
		}
		return true;
	}

	public static GameObject GetEntity(GameObject gameObject)
	{
		if (gameObject == null)
		{
			return null;
		}
		EntityLocator component = gameObject.GetComponent<EntityLocator>();
		if (!component)
		{
			return null;
		}
		return component.entity;
	}
}

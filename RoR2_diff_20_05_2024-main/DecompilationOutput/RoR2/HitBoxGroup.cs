using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class HitBoxGroup : MonoBehaviour
{
	[Tooltip("The name of this hitbox group.")]
	public string groupName;

	[Tooltip("The hitbox objects in this group.")]
	public HitBox[] hitBoxes;

	public static HitBoxGroup FindByGroupName(GameObject gameObject, string groupName)
	{
		List<HitBoxGroup> gameObjectComponents = GetComponentsCache<HitBoxGroup>.GetGameObjectComponents(gameObject);
		HitBoxGroup result = null;
		int i = 0;
		for (int count = gameObjectComponents.Count; i < count; i++)
		{
			if (string.CompareOrdinal(groupName, gameObjectComponents[i].groupName) == 0)
			{
				result = gameObjectComponents[i];
				break;
			}
		}
		GetComponentsCache<HitBoxGroup>.ReturnBuffer(gameObjectComponents);
		return result;
	}
}

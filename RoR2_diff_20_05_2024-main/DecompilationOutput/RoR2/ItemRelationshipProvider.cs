using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/ItemRelationshipProvider")]
public class ItemRelationshipProvider : ScriptableObject
{
	public ItemRelationshipType relationshipType;

	public ItemDef.Pair[] relationships;
}

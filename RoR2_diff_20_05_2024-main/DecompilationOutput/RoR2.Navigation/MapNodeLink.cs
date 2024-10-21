using UnityEngine;

namespace RoR2.Navigation;

[RequireComponent(typeof(MapNode))]
public class MapNodeLink : MonoBehaviour
{
	public MapNode other;

	public float minJumpHeight;

	[Tooltip("The gate name associated with this link. If the named gate is closed, this link will not be used in pathfinding.")]
	public string gateName = "";

	public GameObject[] objectsToEnableDuringTest;

	public GameObject[] objectsToDisableDuringTest;

	private void OnValidate()
	{
		if (other == this)
		{
			Debug.LogWarning("Map node link cannot link a node to itself.");
			other = null;
		}
		if ((bool)other && other.GetComponentInParent<MapNodeGroup>() != GetComponentInParent<MapNodeGroup>())
		{
			Debug.LogWarning("Map node link cannot link to a node in a separate node group.");
			other = null;
		}
	}

	private void OnDrawGizmos()
	{
		if ((bool)other)
		{
			Vector3 position = base.transform.position;
			Vector3 position2 = other.transform.position;
			Vector3 vector = (position + position2) * 0.5f;
			Color yellow = Color.yellow;
			yellow.a = 0.5f;
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(position, vector);
			Gizmos.color = yellow;
			Gizmos.DrawLine(vector, position2);
		}
	}
}

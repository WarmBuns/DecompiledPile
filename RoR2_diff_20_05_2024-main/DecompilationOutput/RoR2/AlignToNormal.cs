using UnityEngine;

namespace RoR2;

public class AlignToNormal : MonoBehaviour
{
	[Tooltip("The amount to raycast down from.")]
	public float maxDistance;

	[Tooltip("The amount to pull the object out of the ground initially to test.")]
	public float offsetDistance;

	[Tooltip("Send to floor only - don't change normals.")]
	public bool changePositionOnly;

	private void Start()
	{
		if (Physics.Raycast(base.transform.position + base.transform.up * offsetDistance, -base.transform.up, out var hitInfo, maxDistance, LayerIndex.world.mask))
		{
			base.transform.position = hitInfo.point;
			if (!changePositionOnly)
			{
				base.transform.up = hitInfo.normal;
			}
		}
	}
}

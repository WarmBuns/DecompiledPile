using UnityEngine;

namespace RoR2;

public class PlacementUtility : MonoBehaviour
{
	public Transform targetParent;

	public GameObject prefabPlacement;

	public bool normalToSurface;

	public bool flipForwardDirection;

	public float minScale = 1f;

	public float maxScale = 2f;

	public float minXRotation;

	public float maxXRotation;

	public float minYRotation;

	public float maxYRotation;

	public float minZRotation;

	public float maxZRotation;

	private void Start()
	{
	}

	private void Update()
	{
	}

	public void PlacePrefab(Vector3 targetPosition, Quaternion rotation)
	{
		_ = (bool)prefabPlacement;
	}
}

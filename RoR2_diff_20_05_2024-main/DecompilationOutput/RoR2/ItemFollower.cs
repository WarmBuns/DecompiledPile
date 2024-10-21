using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(ItemDisplay))]
public class ItemFollower : MonoBehaviour
{
	public GameObject followerPrefab;

	public GameObject targetObject;

	public BezierCurveLine followerCurve;

	public LineRenderer followerLineRenderer;

	public float distanceDampTime;

	public float distanceMaxSpeed;

	private ItemDisplay itemDisplay;

	private Vector3 velocityDistance;

	private Vector3 v0;

	private Vector3 v1;

	[HideInInspector]
	public GameObject followerInstance;

	private void Start()
	{
		itemDisplay = GetComponent<ItemDisplay>();
		followerLineRenderer = GetComponent<LineRenderer>();
		Rebuild();
	}

	private void Rebuild()
	{
		if (itemDisplay.GetVisibilityLevel() == VisibilityLevel.Invisible)
		{
			if ((bool)followerInstance)
			{
				Object.Destroy(followerInstance);
			}
			if ((bool)followerLineRenderer)
			{
				followerLineRenderer.enabled = false;
			}
			return;
		}
		if (!followerInstance)
		{
			followerInstance = Object.Instantiate(followerPrefab, targetObject.transform.position, Quaternion.identity);
			followerInstance.transform.localScale = base.transform.localScale;
			if ((bool)followerCurve)
			{
				v0 = followerCurve.v0;
				v1 = followerCurve.v1;
			}
		}
		if ((bool)followerLineRenderer)
		{
			followerLineRenderer.enabled = true;
		}
	}

	private void Update()
	{
		Rebuild();
		if ((bool)followerInstance)
		{
			Transform transform = followerInstance.transform;
			Transform transform2 = targetObject.transform;
			transform.position = Vector3.SmoothDamp(transform.position, transform2.position, ref velocityDistance, distanceDampTime);
			transform.rotation = transform2.rotation;
			if ((bool)followerCurve)
			{
				followerCurve.v0 = base.transform.TransformVector(v0);
				followerCurve.v1 = transform.TransformVector(v1);
				followerCurve.p1 = transform.position;
			}
		}
	}

	private void OnDestroy()
	{
		if ((bool)followerInstance)
		{
			Object.Destroy(followerInstance);
		}
	}
}

using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(LineRenderer))]
public class LaserPointer : MonoBehaviour
{
	public float laserDistance;

	private LineRenderer line;

	private void Start()
	{
		line = GetComponent<LineRenderer>();
	}

	private void Update()
	{
		if (Physics.Raycast(base.transform.position, base.transform.forward, out var hitInfo, laserDistance, LayerIndex.world.mask))
		{
			line.SetPosition(0, base.transform.position);
			line.SetPosition(1, hitInfo.point);
		}
	}
}

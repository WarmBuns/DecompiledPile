using UnityEngine;

namespace RoR2;

internal class LaserPointerController : MonoBehaviour
{
	public InputBankTest source;

	public GameObject dotObject;

	public LineRenderer beam;

	public float minDistanceFromStart = 4f;

	private void LateUpdate()
	{
		bool flag = false;
		bool active = false;
		if ((bool)source)
		{
			Ray ray = new Ray(source.aimOrigin, source.aimDirection);
			if (Physics.Raycast(ray, out var hitInfo, float.PositiveInfinity, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal))
			{
				base.transform.position = hitInfo.point;
				base.transform.forward = -ray.direction;
				float num = hitInfo.distance - minDistanceFromStart;
				if (num >= 0.1f)
				{
					beam.SetPosition(1, new Vector3(0f, 0f, num));
					flag = true;
				}
				active = true;
			}
		}
		dotObject.SetActive(active);
		beam.enabled = flag;
	}
}

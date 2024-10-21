using UnityEngine;

namespace RoR2;

public class LightScaleFromParent : MonoBehaviour
{
	private void Start()
	{
		Light component = GetComponent<Light>();
		if ((bool)component)
		{
			float range = component.range;
			Vector3 lossyScale = base.transform.lossyScale;
			component.range = range * Mathf.Max(lossyScale.x, lossyScale.y, lossyScale.z);
		}
	}
}

using UnityEngine;

namespace RoR2;

public class SurfaceDefProvider : MonoBehaviour
{
	[Tooltip("The primary surface definition. Use this when not tying to a splatmap.")]
	public SurfaceDef surfaceDef;

	public static SurfaceDef GetObjectSurfaceDef(Collider collider, Vector3 position)
	{
		SurfaceDefProvider component = collider.GetComponent<SurfaceDefProvider>();
		if (!component)
		{
			return null;
		}
		return component.surfaceDef;
	}
}

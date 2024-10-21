using UnityEngine;

namespace RoR2;

[ExecuteAlways]
public class MidpointBetweenTwoTransforms : MonoBehaviour
{
	public Transform transform1;

	public Transform transform2;

	public void Update()
	{
		base.transform.position = Vector3.Lerp(transform1.position, transform2.position, 0.5f);
	}
}

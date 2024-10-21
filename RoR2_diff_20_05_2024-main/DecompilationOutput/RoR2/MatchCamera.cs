using UnityEngine;

namespace RoR2;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class MatchCamera : MonoBehaviour
{
	private Camera destCamera;

	public Camera srcCamera;

	public bool matchFOV = true;

	public bool matchRect = true;

	public bool matchPosition;

	private void Awake()
	{
		destCamera = GetComponent<Camera>();
	}

	private void LateUpdate()
	{
		if ((bool)srcCamera)
		{
			if (matchRect)
			{
				destCamera.rect = srcCamera.rect;
			}
			if (matchFOV)
			{
				destCamera.fieldOfView = srcCamera.fieldOfView;
			}
			if (matchPosition)
			{
				destCamera.transform.position = srcCamera.transform.position;
				destCamera.transform.rotation = srcCamera.transform.rotation;
			}
		}
	}
}

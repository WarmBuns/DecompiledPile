using System.Collections.ObjectModel;
using UnityEngine;

namespace RoR2;

public class ForcedCamera : MonoBehaviour, ICameraStateProvider
{
	public float entryLerpDuration = 1f;

	public float exitLerpDuration = 1f;

	public float fovOverride;

	public bool allowUserLook;

	public bool allowUserHud;

	private void Update()
	{
		ReadOnlyCollection<CameraRigController> readOnlyInstancesList = CameraRigController.readOnlyInstancesList;
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			CameraRigController cameraRigController = readOnlyInstancesList[i];
			if (!cameraRigController.hasOverride)
			{
				cameraRigController.SetOverrideCam(this, entryLerpDuration);
			}
		}
	}

	private void OnDisable()
	{
		ReadOnlyCollection<CameraRigController> readOnlyInstancesList = CameraRigController.readOnlyInstancesList;
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			CameraRigController cameraRigController = readOnlyInstancesList[i];
			if (cameraRigController.IsOverrideCam(this))
			{
				cameraRigController.SetOverrideCam(null, exitLerpDuration);
			}
		}
	}

	public void GetCameraState(CameraRigController cameraRigController, ref CameraState cameraState)
	{
		cameraState.position = base.transform.position;
		cameraState.rotation = base.transform.rotation;
		if (fovOverride > 0f)
		{
			cameraState.fov = fovOverride;
		}
	}

	public bool IsUserLookAllowed(CameraRigController cameraRigController)
	{
		return allowUserLook;
	}

	public bool IsUserControlAllowed(CameraRigController cameraRigController)
	{
		return false;
	}

	public bool IsHudAllowed(CameraRigController cameraRigController)
	{
		return allowUserHud;
	}

	private void OnDrawGizmosSelected()
	{
		Color color = Gizmos.color;
		Matrix4x4 matrix = Gizmos.matrix;
		Gizmos.color = Color.yellow;
		Matrix4x4 identity = Matrix4x4.identity;
		identity.SetTRS(base.transform.position, base.transform.rotation, Vector3.one);
		Gizmos.matrix = identity;
		Gizmos.DrawFrustum(Vector3.zero, (fovOverride > 0f) ? fovOverride : 60f, 10f, 0.1f, 1.7777778f);
		Gizmos.matrix = matrix;
		Gizmos.color = color;
	}
}

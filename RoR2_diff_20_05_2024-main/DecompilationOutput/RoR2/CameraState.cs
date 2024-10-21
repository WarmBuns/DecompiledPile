using UnityEngine;

namespace RoR2;

public struct CameraState
{
	public Vector3 position;

	public Quaternion rotation;

	public float fov;

	public static CameraState Lerp(ref CameraState a, ref CameraState b, float t)
	{
		CameraState result = default(CameraState);
		result.position = Vector3.LerpUnclamped(a.position, b.position, t);
		result.rotation = Quaternion.SlerpUnclamped(a.rotation, b.rotation, t);
		result.fov = Mathf.LerpUnclamped(a.fov, b.fov, t);
		return result;
	}

	public static CameraState SmoothDamp(CameraState current, CameraState target, ref Vector3 positionVelocity, ref float angleVelocity, ref float fovVelocity, float smoothTime)
	{
		CameraState result = default(CameraState);
		result.position = Vector3.SmoothDamp(current.position, target.position, ref positionVelocity, smoothTime);
		result.rotation = Util.SmoothDampQuaternion(current.rotation, target.rotation, ref angleVelocity, smoothTime);
		result.fov = Mathf.SmoothDamp(current.fov, target.fov, ref fovVelocity, smoothTime);
		return result;
	}
}

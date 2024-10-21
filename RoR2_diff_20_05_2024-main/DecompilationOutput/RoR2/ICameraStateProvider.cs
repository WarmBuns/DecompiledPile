namespace RoR2;

public interface ICameraStateProvider
{
	void GetCameraState(CameraRigController cameraRigController, ref CameraState cameraState);

	bool IsUserLookAllowed(CameraRigController cameraRigController);

	bool IsUserControlAllowed(CameraRigController cameraRigController);

	bool IsHudAllowed(CameraRigController cameraRigController);
}

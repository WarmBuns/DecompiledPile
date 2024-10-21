namespace RoR2.GamepadVibration;

public struct VibrationContext
{
	public CameraRigController cameraRigController;

	public LocalUser localUser;

	public float userVibrationScale;

	public float CalcCamDisplacementMagnitude()
	{
		if (!cameraRigController || !Run.instance || !Run.instance.isRunning || localUser.cachedMasterController.master.lostBodyToDeath)
		{
			return 0f;
		}
		return cameraRigController.rawScreenShakeDisplacement.magnitude;
	}
}

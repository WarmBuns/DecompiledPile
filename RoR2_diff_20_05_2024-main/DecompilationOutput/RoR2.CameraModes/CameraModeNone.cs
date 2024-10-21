using UnityEngine;

namespace RoR2.CameraModes;

public class CameraModeNone : CameraModeBase
{
	public static readonly CameraModeNone instance;

	public override bool IsSpectating(CameraRigController cameraRigController)
	{
		return true;
	}

	protected override void ApplyLookInputInternal(object rawInstanceData, in CameraModeContext context, in ApplyLookInputArgs input)
	{
	}

	protected override void CollectLookInputInternal(object rawInstanceData, in CameraModeContext context, out CollectLookInputResult result)
	{
		result.lookInput = Vector2.zero;
	}

	protected override object CreateInstanceData(CameraRigController cameraRigController)
	{
		return null;
	}

	protected override void MatchStateInternal(object rawInstanceData, in CameraModeContext context, in CameraState cameraStateToMatch)
	{
	}

	protected override void OnTargetChangedInternal(object rawInstanceData, CameraRigController cameraRigController, in OnTargetChangedArgs args)
	{
	}

	protected override void UpdateInternal(object rawInstanceData, in CameraModeContext context, out UpdateResult result)
	{
		result.cameraState = context.cameraInfo.previousCameraState;
		result.firstPersonTarget = null;
		result.showSprintParticles = false;
		result.showDisabledSkillsParticles = false;
		result.crosshairWorldPosition = result.cameraState.position;
	}
}

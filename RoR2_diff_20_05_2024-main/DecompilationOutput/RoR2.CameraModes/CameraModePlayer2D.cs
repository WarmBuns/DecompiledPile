using UnityEngine;

namespace RoR2.CameraModes;

public class CameraModePlayer2D : CameraModeBase
{
	private class InstanceData
	{
		public float facing = 1f;
	}

	public static readonly CameraModePlayer2D instance = new CameraModePlayer2D();

	public override bool IsSpectating(CameraRigController cameraRigController)
	{
		return false;
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
		return new InstanceData();
	}

	protected override void MatchStateInternal(object rawInstanceData, in CameraModeContext context, in CameraState cameraStateToMatch)
	{
	}

	protected override void OnTargetChangedInternal(object rawInstanceData, CameraRigController cameraRigController, in OnTargetChangedArgs args)
	{
	}

	protected override void UpdateInternal(object rawInstanceData, in CameraModeContext context, out UpdateResult result)
	{
		InstanceData instanceData = (InstanceData)rawInstanceData;
		result.cameraState = context.cameraInfo.previousCameraState;
		result.firstPersonTarget = null;
		result.showSprintParticles = false;
		result.showDisabledSkillsParticles = false;
		result.crosshairWorldPosition = result.cameraState.position;
		if (!context.targetInfo.target)
		{
			return;
		}
		Quaternion identity = Quaternion.identity;
		Vector3 vector = identity * Quaternion.Euler(0f, 90f, 0f) * Vector3.forward;
		Quaternion quaternion = ((!context.targetInfo.body || !context.targetInfo.body.characterDirection) ? context.targetInfo.target.transform.rotation : Quaternion.Euler(0f, context.targetInfo.body.characterDirection.yaw, 0f));
		Vector3 vector2 = context.targetInfo.target.transform.position;
		if ((bool)context.targetInfo.inputBank)
		{
			float num = Vector3.Dot(vector, context.targetInfo.inputBank.moveVector);
			if (num != 0f)
			{
				instanceData.facing = num;
			}
			vector2 = context.targetInfo.inputBank.aimOrigin;
		}
		_ = quaternion * Quaternion.Euler(0f, 90f, 0f);
		result.cameraState.rotation = identity;
		result.cameraState.position = context.targetInfo.target.transform.position + new Vector3(0f, 10f, 0f) + identity * Vector3.forward * -30f;
		result.cameraState.fov = 60f;
		result.crosshairWorldPosition = vector2 + vector * instanceData.facing * 30f;
	}
}

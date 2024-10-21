using Rewired;
using UnityEngine;

namespace RoR2.CameraModes;

public class CameraModePlayerBasic : CameraModeBase
{
	protected class InstanceData
	{
		public float currentCameraDistance;

		public float cameraDistanceVelocity;

		public float stickAimPreviousAcceleratedMagnitude;

		public float minPitch;

		public float maxPitch;

		public PitchYawPair pitchYaw;

		public CameraRigController.AimAssistInfo lastAimAssist;

		public CameraRigController.AimAssistInfo aimAssist;

		public HurtBox lastCrosshairHurtBox;

		public bool hasEverHadTarget;

		public float neutralFov;

		public float neutralFovVelocity;

		public void SetPitchYawFromLookVector(Vector3 lookVector)
		{
			Vector3 vector = lookVector;
			float x = Mathf.Sqrt(vector.x * vector.x + vector.z * vector.z);
			pitchYaw.pitch = Mathf.Atan2(0f - vector.y, x) * 57.29578f;
			pitchYaw.yaw = Mathf.Repeat(Mathf.Atan2(vector.x, vector.z) * 57.29578f, 360f);
		}
	}

	public SurvivorIndex targetSurvivorClass;

	private bool skipFirst;

	public bool isSpectatorMode;

	public Vector3 prevGyroAngle;

	public static CameraModePlayerBasic playerBasic = new CameraModePlayerBasic
	{
		isSpectatorMode = false
	};

	public static CameraModePlayerBasic spectator = new CameraModePlayerBasic
	{
		isSpectatorMode = true
	};

	protected override object CreateInstanceData(CameraRigController cameraRigController)
	{
		return new InstanceData();
	}

	protected override void OnInstallInternal(object rawInstancedata, CameraRigController cameraRigController)
	{
		base.OnInstallInternal(rawInstancedata, cameraRigController);
		((InstanceData)rawInstancedata).neutralFov = cameraRigController.baseFov;
	}

	public override bool IsSpectating(CameraRigController cameraRigController)
	{
		return isSpectatorMode;
	}

	protected override void UpdateInternal(object rawInstanceData, in CameraModeContext context, out UpdateResult result)
	{
		InstanceData instanceData = (InstanceData)rawInstanceData;
		CameraRigController cameraRigController = context.cameraInfo.cameraRigController;
		CameraTargetParams targetParams = context.targetInfo.targetParams;
		float fov = context.cameraInfo.baseFov;
		Quaternion quaternion = context.cameraInfo.previousCameraState.rotation;
		Vector3 position = context.cameraInfo.previousCameraState.position;
		GameObject firstPersonTarget = null;
		float num = cameraRigController.baseFov;
		if (context.targetInfo.isSprinting)
		{
			num *= 1.3f;
		}
		instanceData.neutralFov = Mathf.SmoothDamp(instanceData.neutralFov, num, ref instanceData.neutralFovVelocity, 0.2f, float.PositiveInfinity, Time.deltaTime);
		CharacterCameraParamsData dest = CharacterCameraParamsData.basic;
		dest.fov = instanceData.neutralFov;
		Vector2 vector = Vector2.zero;
		if ((bool)targetParams)
		{
			CharacterCameraParamsData.Blend(in targetParams.currentCameraParamsData, ref dest, 1f);
			fov = dest.fov.value;
			vector = targetParams.recoil;
		}
		if (dest.isFirstPerson.value)
		{
			firstPersonTarget = context.targetInfo.target;
		}
		instanceData.minPitch = dest.minPitch.value;
		instanceData.maxPitch = dest.maxPitch.value;
		float pitch = instanceData.pitchYaw.pitch;
		float yaw = instanceData.pitchYaw.yaw;
		pitch += vector.y;
		yaw += vector.x;
		pitch = Mathf.Clamp(pitch, instanceData.minPitch, instanceData.maxPitch);
		yaw = Mathf.Repeat(yaw, 360f);
		Vector3 targetPivotPosition = CalculateTargetPivotPosition(in context);
		if ((bool)context.targetInfo.target)
		{
			quaternion = Quaternion.Euler(pitch, yaw, 0f);
			Vector3 direction = targetPivotPosition + quaternion * dest.idealLocalCameraPos.value - targetPivotPosition;
			float magnitude = direction.magnitude;
			float num2 = (1f + pitch / -90f) * 0.5f;
			magnitude *= Mathf.Sqrt(1f - num2);
			if (magnitude < 0.25f)
			{
				magnitude = 0.25f;
			}
			Ray ray = new Ray(targetPivotPosition, direction);
			float num3 = cameraRigController.Raycast(new Ray(targetPivotPosition, direction), magnitude, dest.wallCushion.value - 0.01f);
			Debug.DrawRay(ray.origin, ray.direction * magnitude, Color.yellow, Time.deltaTime);
			Debug.DrawRay(ray.origin, ray.direction * num3, Color.red, Time.deltaTime);
			if (instanceData.currentCameraDistance >= num3)
			{
				instanceData.currentCameraDistance = num3;
				instanceData.cameraDistanceVelocity = 0f;
			}
			else
			{
				instanceData.currentCameraDistance = Mathf.SmoothDamp(instanceData.currentCameraDistance, num3, ref instanceData.cameraDistanceVelocity, 0.5f);
			}
			position = targetPivotPosition + direction.normalized * instanceData.currentCameraDistance;
		}
		result.cameraState.position = position;
		result.cameraState.rotation = quaternion;
		result.cameraState.fov = fov;
		result.showSprintParticles = context.targetInfo.isSprinting;
		result.showDisabledSkillsParticles = context.targetInfo.skillsAreDisabled;
		result.firstPersonTarget = firstPersonTarget;
		UpdateCrosshair(rawInstanceData, in context, in result.cameraState, in targetPivotPosition, out result.crosshairWorldPosition);
	}

	protected override void CollectLookInputInternal(object rawInstanceData, in CameraModeContext context, out CollectLookInputResult output)
	{
		InstanceData instanceData = (InstanceData)rawInstanceData;
		ref readonly CameraInfo cameraInfo = ref context.cameraInfo;
		ref readonly ViewerInfo viewerInfo = ref context.viewerInfo;
		ref readonly TargetInfo targetInfo = ref context.targetInfo;
		bool hasCursor = viewerInfo.hasCursor;
		Player inputPlayer = viewerInfo.inputPlayer;
		UserProfile userProfile = viewerInfo.userProfile;
		ICameraStateProvider overrideCam = cameraInfo.overrideCam;
		output.lookInput = Vector3.zero;
		if (!hasCursor && viewerInfo.inputPlayer != null && userProfile != null && !viewerInfo.isUIFocused && (!(Object)overrideCam || overrideCam.IsUserLookAllowed(cameraInfo.cameraRigController)))
		{
			Vector2 vector = new Vector2(inputPlayer.GetAxisRaw(2), inputPlayer.GetAxisRaw(3));
			Vector2 aimStickVector = new Vector2(inputPlayer.GetAxisRaw(16), inputPlayer.GetAxisRaw(17));
			ConditionalNegate(ref vector.x, userProfile.mouseLookInvertX);
			ConditionalNegate(ref vector.y, userProfile.mouseLookInvertY);
			ConditionalNegate(ref aimStickVector.x, userProfile.stickLookInvertX);
			ConditionalNegate(ref aimStickVector.y, userProfile.stickLookInvertY);
			PerformStickPostProcessing(instanceData, in context, ref aimStickVector);
			float mouseLookSensitivity = userProfile.mouseLookSensitivity;
			float num = userProfile.stickLookSensitivity * CameraRigController.aimStickGlobalScale.value * 45f;
			Vector2 vector2 = new Vector2(userProfile.mouseLookScaleX, userProfile.mouseLookScaleY);
			Vector2 vector3 = new Vector2(userProfile.stickLookScaleX, userProfile.stickLookScaleY);
			vector *= vector2 * mouseLookSensitivity;
			aimStickVector *= vector3 * num;
			PerformAimAssist(in context, ref aimStickVector);
			aimStickVector *= Time.deltaTime;
			output.lookInput = vector + aimStickVector;
			if (userProfile.gyroEnabled)
			{
				Vector3 gyroAimDeltaVector = PlatformSystems.platformManager.GetGyroAimDeltaVector();
				bool flag = false;
				flag = ((targetSurvivorClass == RoR2Content.Survivors.Huntress.survivorIndex) ? userProfile.gyroDisabledDuringSprint_Huntress : ((targetSurvivorClass != RoR2Content.Survivors.Loader.survivorIndex && targetSurvivorClass != RoR2Content.Survivors.Merc.survivorIndex && targetSurvivorClass != RoR2Content.Survivors.Croco.survivorIndex) ? userProfile.gyroDisabledDuringSprint : userProfile.gyroDisabledDuringSprint_Melee));
				if (!context.targetInfo.isSprinting || !flag)
				{
					ConditionalNegate(ref gyroAimDeltaVector.x, userProfile.gyroLookInvertY);
					ConditionalNegate(ref gyroAimDeltaVector.y, userProfile.gyroLookInvertX);
					if (skipFirst)
					{
						output.lookInput += new Vector2((0f - gyroAimDeltaVector.y) * userProfile.gyroLookSensitivity * userProfile.gyroLookScaleX * 1250f, gyroAimDeltaVector.x * userProfile.gyroLookSensitivity * userProfile.gyroLookScaleY * 1250f);
					}
					else
					{
						skipFirst = true;
					}
				}
			}
		}
		output.lookInput *= cameraInfo.previousCameraState.fov / cameraInfo.baseFov;
		if (targetInfo.isSprinting && CameraRigController.enableSprintSensitivitySlowdown.value)
		{
			output.lookInput *= 0.5f;
		}
		static void ConditionalNegate(ref float value, bool condition)
		{
			value = (condition ? (0f - value) : value);
		}
	}

	private void PerformStickPostProcessing(InstanceData instanceData, in CameraModeContext context, ref Vector2 aimStickVector)
	{
		float magnitude = aimStickVector.magnitude;
		float num = magnitude;
		_ = Vector2.zero;
		_ = Vector2.zero;
		_ = Vector2.zero;
		if (CameraRigController.aimStickDualZoneSmoothing.value != 0f)
		{
			float maxDelta = Time.deltaTime / CameraRigController.aimStickDualZoneSmoothing.value;
			num = (instanceData.stickAimPreviousAcceleratedMagnitude = Mathf.Min(Mathf.MoveTowards(instanceData.stickAimPreviousAcceleratedMagnitude, magnitude, maxDelta), magnitude));
			if (magnitude == 0f)
			{
				_ = Vector2.zero;
			}
			else
			{
				_ = aimStickVector * (num / magnitude);
			}
		}
		float num2 = num;
		float value = CameraRigController.aimStickDualZoneSlope.value;
		float num3 = ((!(num2 <= CameraRigController.aimStickDualZoneThreshold.value)) ? (1f - value) : 0f);
		num = value * num2 + num3;
		if (magnitude == 0f)
		{
			_ = Vector2.zero;
		}
		else
		{
			_ = aimStickVector * (num / magnitude);
		}
		num = Mathf.Pow(num, CameraRigController.aimStickExponent.value);
		if (magnitude == 0f)
		{
			_ = Vector2.zero;
		}
		else
		{
			_ = aimStickVector * (num / magnitude);
		}
		if (magnitude != 0f)
		{
			aimStickVector *= num / magnitude;
		}
	}

	private void PerformAimAssist(in CameraModeContext context, ref Vector2 aimStickVector)
	{
		ref readonly TargetInfo targetInfo = ref context.targetInfo;
		ref readonly CameraInfo cameraInfo = ref context.cameraInfo;
		ref readonly UserProfile userProfile = ref context.viewerInfo.userProfile;
		if (!context.targetInfo.isViewerControlled || context.targetInfo.isSprinting || userProfile.inputAimAssistSensitivity == 0f)
		{
			return;
		}
		Camera sceneCam = cameraInfo.sceneCam;
		if (!userProfile.aimAssistEnabled)
		{
			return;
		}
		AimAssistTarget aimAssistTarget = null;
		AimAssistTarget aimAssistTarget2 = null;
		float value = CameraRigController.aimStickAssistMinSize.value;
		float num = value * CameraRigController.aimStickAssistMaxSize.value;
		Util.Remap(userProfile.inputAimAssistSensitivity, 1f, 10f, 0.4f, 0.15f);
		float value2 = CameraRigController.aimStickAssistMinSlowdownScale.value;
		float num2 = 0f;
		float num3 = 0f;
		Vector2 vector = Vector2.zero;
		Vector2 normalized = aimStickVector.normalized;
		Vector2 vector2 = new Vector2(0.5f, 0.5f);
		for (int i = 0; i < AimAssistTarget.instancesList.Count; i++)
		{
			AimAssistTarget aimAssistTarget3 = AimAssistTarget.instancesList[i];
			if (aimAssistTarget3.teamComponent.teamIndex == targetInfo.teamIndex)
			{
				continue;
			}
			Vector3 vA = sceneCam.WorldToViewportPoint(aimAssistTarget3.point0.position);
			Vector3 vB = sceneCam.WorldToViewportPoint(aimAssistTarget3.point1.position);
			float num4 = Mathf.Lerp(vA.z, vB.z, 0.5f);
			if (!(num4 <= 18f))
			{
				float num5 = 1f / num4;
				Vector2 vector3 = (Vector2)Util.ClosestPointOnLine(vA, vB, vector2) - vector2;
				float num6 = Mathf.Clamp01(Util.Remap(vector3.magnitude, value * aimAssistTarget3.assistScale * num5, num * aimAssistTarget3.assistScale * num5, 1f, 0f));
				float num7 = Mathf.Clamp01(Vector3.Dot(vector3, normalized));
				if (num6 > num2)
				{
					num2 = num6;
					aimAssistTarget2 = aimAssistTarget3;
					num3 = num7;
					aimAssistTarget = aimAssistTarget3;
					vector = vector3;
				}
			}
		}
		Vector2 vector4 = aimStickVector;
		if ((bool)aimAssistTarget2)
		{
			float outMin = Util.Remap(userProfile.aimAssistSensitivity, 1f, 10f, 0.4f, 0.15f);
			float num8 = Mathf.Clamp01(Util.Remap(1f - num2, 0f, 1f, outMin, value2));
			vector4 *= num8;
		}
		if ((bool)aimAssistTarget && num3 != 0f)
		{
			float maxRadiansDelta = Util.Remap(num3, 0f, 1f, CameraRigController.aimStickAssistMinDelta.value, CameraRigController.aimStickAssistMaxDelta.value);
			vector4 = Vector3.RotateTowards(vector4, vector, maxRadiansDelta, 0f);
		}
		aimStickVector = vector4;
	}

	protected override void ApplyLookInputInternal(object rawInstanceData, in CameraModeContext context, in ApplyLookInputArgs input)
	{
		InstanceData instanceData = (InstanceData)rawInstanceData;
		ref readonly TargetInfo targetInfo = ref context.targetInfo;
		if (targetInfo.isViewerControlled)
		{
			float minPitch = instanceData.minPitch;
			float maxPitch = instanceData.maxPitch;
			instanceData.pitchYaw.pitch = Mathf.Clamp(instanceData.pitchYaw.pitch - input.lookInput.y, minPitch, maxPitch);
			instanceData.pitchYaw.yaw += input.lookInput.x;
			if ((bool)targetInfo.networkedViewAngles && targetInfo.networkedViewAngles.hasEffectiveAuthority)
			{
				targetInfo.networkedViewAngles.viewAngles = instanceData.pitchYaw;
			}
		}
		else if ((bool)targetInfo.networkedViewAngles)
		{
			instanceData.pitchYaw = targetInfo.networkedViewAngles.viewAngles;
		}
		else if ((bool)targetInfo.inputBank)
		{
			instanceData.SetPitchYawFromLookVector(targetInfo.inputBank.aimDirection);
		}
	}

	protected void UpdateCrosshair(object rawInstanceData, in CameraModeContext context, in CameraState cameraState, in Vector3 targetPivotPosition, out Vector3 crosshairWorldPosition)
	{
		InstanceData instanceData = (InstanceData)rawInstanceData;
		instanceData.lastAimAssist = instanceData.aimAssist;
		Ray crosshairRaycastRay = GetCrosshairRaycastRay(in context, Vector2.zero, targetPivotPosition, in cameraState);
		bool flag = false;
		instanceData.lastCrosshairHurtBox = null;
		RaycastHit raycastHit = default(RaycastHit);
		int num = 0;
		num = HGPhysics.RaycastAll(out var hits, crosshairRaycastRay.origin, crosshairRaycastRay.direction, context.cameraInfo.cameraRigController.maxAimRaycastDistance, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore);
		float num2 = float.PositiveInfinity;
		int num3 = -1;
		for (int i = 0; i < num; i++)
		{
			RaycastHit raycastHit2 = hits[i];
			HurtBox hurtBox = raycastHit2.collider.GetComponent<HurtBox>();
			EntityLocator component = raycastHit2.collider.GetComponent<EntityLocator>();
			float distance = raycastHit2.distance;
			if (distance <= 3f || num2 <= distance)
			{
				continue;
			}
			if ((bool)hurtBox)
			{
				if (hurtBox.teamIndex == context.targetInfo.teamIndex)
				{
					continue;
				}
				if ((bool)hurtBox.healthComponent && hurtBox.healthComponent.dontShowHealthbar)
				{
					hurtBox = null;
				}
			}
			if ((bool)component)
			{
				VehicleSeat vehicleSeat = (component.entity ? component.entity.GetComponent<VehicleSeat>() : null);
				if ((bool)vehicleSeat && vehicleSeat.currentPassengerBody == context.targetInfo.body)
				{
					continue;
				}
			}
			num2 = distance;
			num3 = i;
			instanceData.lastCrosshairHurtBox = hurtBox;
		}
		if (num3 != -1)
		{
			flag = true;
			raycastHit = hits[num3];
		}
		HGPhysics.ReturnResults(hits);
		instanceData.aimAssist.aimAssistHurtbox = null;
		if (flag)
		{
			crosshairWorldPosition = raycastHit.point;
			float num4 = 1000f;
			if (!(raycastHit.distance < num4))
			{
				return;
			}
			HurtBox component2 = raycastHit.collider.GetComponent<HurtBox>();
			if (!component2)
			{
				return;
			}
			HealthComponent healthComponent = component2.healthComponent;
			if (!healthComponent)
			{
				return;
			}
			TeamComponent component3 = healthComponent.GetComponent<TeamComponent>();
			if ((bool)component3 && component3.teamIndex != context.targetInfo.teamIndex && component3.teamIndex != TeamIndex.None)
			{
				HurtBox hurtBox2 = healthComponent.body?.mainHurtBox;
				if ((bool)hurtBox2)
				{
					instanceData.aimAssist.aimAssistHurtbox = hurtBox2;
					instanceData.aimAssist.worldPosition = raycastHit.point;
					instanceData.aimAssist.localPositionOnHurtbox = hurtBox2.transform.InverseTransformPoint(raycastHit.point);
				}
			}
		}
		else
		{
			crosshairWorldPosition = crosshairRaycastRay.GetPoint(context.cameraInfo.cameraRigController.maxAimRaycastDistance);
		}
	}

	private Ray GetCrosshairRaycastRay(in CameraModeContext context, Vector2 crosshairOffset, Vector3 raycastStartPlanePoint, in CameraState cameraState)
	{
		if (!context.cameraInfo.sceneCam)
		{
			return default(Ray);
		}
		float fov = cameraState.fov;
		float num = fov * context.cameraInfo.sceneCam.aspect;
		Quaternion quaternion = Quaternion.Euler(crosshairOffset.y * fov, crosshairOffset.x * num, 0f);
		quaternion = cameraState.rotation * quaternion;
		return new Ray(Vector3.ProjectOnPlane(cameraState.position - raycastStartPlanePoint, cameraState.rotation * Vector3.forward) + raycastStartPlanePoint, quaternion * Vector3.forward);
	}

	private Vector3 CalculateTargetPivotPosition(in CameraModeContext context)
	{
		CameraRigController cameraRigController = context.cameraInfo.cameraRigController;
		CameraTargetParams targetParams = context.targetInfo.targetParams;
		Vector3 result = context.cameraInfo.previousCameraState.position;
		if ((bool)targetParams)
		{
			Vector3 position = targetParams.transform.position;
			Vector3 vector = (targetParams.cameraPivotTransform ? targetParams.cameraPivotTransform.position : position) + new Vector3(0f, targetParams.currentCameraParamsData.pivotVerticalOffset.value, 0f);
			if (targetParams.dontRaycastToPivot)
			{
				result = vector;
			}
			else
			{
				Vector3 direction = vector - position;
				float magnitude = direction.magnitude;
				Ray ray = new Ray(position, direction);
				float num = cameraRigController.Raycast(ray, magnitude, targetParams.currentCameraParamsData.wallCushion.value);
				Debug.DrawRay(ray.origin, ray.direction * magnitude, Color.green, Time.deltaTime);
				Debug.DrawRay(ray.origin, ray.direction * num, Color.red, Time.deltaTime);
				result = ray.GetPoint(num);
			}
		}
		return result;
	}

	protected override void OnTargetChangedInternal(object rawInstanceData, CameraRigController cameraRigController, in OnTargetChangedArgs args)
	{
		InstanceData instanceData = (InstanceData)rawInstanceData;
		if (instanceData.hasEverHadTarget)
		{
			return;
		}
		if ((bool)args.newTarget)
		{
			CharacterBody component = args.newTarget.GetComponent<CharacterBody>();
			if ((bool)component)
			{
				CharacterDirection characterDirection = component.characterDirection;
				if ((bool)characterDirection)
				{
					instanceData.pitchYaw = new PitchYawPair(0f, characterDirection.yaw);
				}
				targetSurvivorClass = SurvivorCatalog.GetSurvivorIndexFromBodyIndex(component.bodyIndex);
			}
			instanceData.hasEverHadTarget = true;
		}
		else
		{
			targetSurvivorClass = SurvivorIndex.None;
		}
	}

	protected override void MatchStateInternal(object rawInstanceData, in CameraModeContext context, in CameraState cameraStateToMatch)
	{
		((InstanceData)rawInstanceData).SetPitchYawFromLookVector(cameraStateToMatch.rotation * Vector3.forward);
	}
}

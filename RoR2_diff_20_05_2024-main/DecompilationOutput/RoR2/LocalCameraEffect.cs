using System.Collections.Generic;
using RoR2.CameraModes;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace RoR2;

public class LocalCameraEffect : MonoBehaviour
{
	public GameObject targetCharacter;

	public GameObject effectRoot;

	private static List<LocalCameraEffect> instancesList;

	static LocalCameraEffect()
	{
		instancesList = new List<LocalCameraEffect>();
		RoR2Application.onUpdate += CullNonLocalCameraEffects;
	}

	private static void CullNonLocalCameraEffects()
	{
		foreach (LocalCameraEffect instances in instancesList)
		{
			instances.effectRoot.SetActive(instances.TryFindLocalUserCameraRigController() != null);
		}
	}

	private CameraRigController TryFindLocalUserCameraRigController()
	{
		foreach (LocalUser readOnlyLocalUsers in LocalUserManager.readOnlyLocalUsersList)
		{
			CameraRigController cameraRigController = readOnlyLocalUsers.cameraRigController;
			if ((bool)cameraRigController && cameraRigController.target == targetCharacter && cameraRigController.cameraMode == CameraModePlayerBasic.playerBasic)
			{
				return cameraRigController;
			}
		}
		return null;
	}

	private void Start()
	{
		instancesList.Add(this);
		PostProcessVolume componentInChildren = effectRoot.GetComponentInChildren<PostProcessVolume>();
		if (componentInChildren != null)
		{
			CameraRigController cameraRigController = TryFindLocalUserCameraRigController();
			if (cameraRigController != null)
			{
				cameraRigController.SetPostProcessVolumeExclusive(componentInChildren);
			}
		}
	}

	private void OnDestroy()
	{
		instancesList.Remove(this);
	}
}

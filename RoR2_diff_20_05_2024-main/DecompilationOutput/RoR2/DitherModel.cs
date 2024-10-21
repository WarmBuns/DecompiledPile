using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class DitherModel : MonoBehaviour
{
	[HideInInspector]
	public float fade = 1f;

	public float oldFade = 1f;

	public Collider bounds;

	public Renderer[] renderers;

	private MaterialPropertyBlock propertyStorage;

	private static List<DitherModel> instancesList;

	static DitherModel()
	{
		instancesList = new List<DitherModel>();
		SceneCamera.onSceneCameraPreRender += OnSceneCameraPreRender;
	}

	private static void OnSceneCameraPreRender(SceneCamera sceneCamera)
	{
		if ((bool)sceneCamera.cameraRigController)
		{
			RefreshObstructorsForCamera(sceneCamera.cameraRigController);
		}
	}

	private static void RefreshObstructorsForCamera(CameraRigController cameraRigController)
	{
		Vector3 position = cameraRigController.transform.position;
		for (int i = 0; i < instancesList.Count; i++)
		{
			DitherModel ditherModel = instancesList[i];
			if ((bool)ditherModel.bounds)
			{
				Vector3 a = ditherModel.bounds.ClosestPointOnBounds(position);
				if (cameraRigController.enableFading)
				{
					ditherModel.fade = Mathf.Clamp01(Util.Remap(Vector3.Distance(a, position), cameraRigController.fadeStartDistance, cameraRigController.fadeEndDistance, 0f, 1f));
				}
				else
				{
					ditherModel.fade = 1f;
				}
				ditherModel.UpdateDither();
			}
			else
			{
				Debug.LogFormat("{0} has missing collider for dither model", ditherModel.gameObject);
			}
		}
	}

	private void UpdateDither()
	{
		if (fade != oldFade)
		{
			for (int num = renderers.Length - 1; num >= 0; num--)
			{
				Renderer obj = renderers[num];
				obj.GetPropertyBlock(propertyStorage);
				propertyStorage.SetFloat("_Fade", fade);
				obj.SetPropertyBlock(propertyStorage);
			}
		}
		oldFade = fade;
	}

	private void Awake()
	{
		propertyStorage = new MaterialPropertyBlock();
	}

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}
}

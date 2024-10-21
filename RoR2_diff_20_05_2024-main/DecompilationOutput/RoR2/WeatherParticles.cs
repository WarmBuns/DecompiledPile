using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class WeatherParticles : MonoBehaviour
{
	public bool resetPositionToZero;

	public bool lockPosition = true;

	public bool lockRotation = true;

	private static List<WeatherParticles> instancesList;

	static WeatherParticles()
	{
		instancesList = new List<WeatherParticles>();
		SceneCamera.onSceneCameraPreRender += OnSceneCameraPreRender;
	}

	private void UpdateForCamera(CameraRigController cameraRigController, bool lockPosition, bool lockRotation)
	{
		Transform transform = cameraRigController.transform;
		base.transform.SetPositionAndRotation(lockPosition ? transform.position : base.transform.position, lockRotation ? transform.rotation : base.transform.rotation);
	}

	private static void OnSceneCameraPreRender(SceneCamera sceneCamera)
	{
		if ((bool)sceneCamera.cameraRigController)
		{
			for (int i = 0; i < instancesList.Count; i++)
			{
				WeatherParticles weatherParticles = instancesList[i];
				weatherParticles.UpdateForCamera(sceneCamera.cameraRigController, weatherParticles.lockPosition, weatherParticles.lockRotation);
			}
		}
	}

	private void OnEnable()
	{
		instancesList.Add(this);
		if (resetPositionToZero)
		{
			base.transform.position = Vector3.zero;
		}
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}
}

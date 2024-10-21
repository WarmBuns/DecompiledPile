using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class Billboard : MonoBehaviour
{
	private static List<Transform> instanceTransformsList;

	static Billboard()
	{
		instanceTransformsList = new List<Transform>();
		SceneCamera.onSceneCameraPreCull += OnSceneCameraPreCull;
	}

	private static void OnSceneCameraPreCull(SceneCamera sceneCamera)
	{
		Quaternion rotation = sceneCamera.transform.rotation;
		for (int i = 0; i < instanceTransformsList.Count; i++)
		{
			instanceTransformsList[i].rotation = rotation;
		}
	}

	private void OnEnable()
	{
		instanceTransformsList.Add(base.transform);
	}

	private void OnDisable()
	{
		instanceTransformsList.Remove(base.transform);
	}
}

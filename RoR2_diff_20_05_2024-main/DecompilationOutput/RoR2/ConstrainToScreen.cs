using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class ConstrainToScreen : MonoBehaviour
{
	private static float boundaryUVSize;

	private static List<Transform> instanceTransformsList;

	static ConstrainToScreen()
	{
		boundaryUVSize = 0.05f;
		instanceTransformsList = new List<Transform>();
		SceneCamera.onSceneCameraPreCull += OnSceneCameraPreCull;
	}

	private static void OnSceneCameraPreCull(SceneCamera sceneCamera)
	{
		Camera camera = sceneCamera.camera;
		for (int i = 0; i < instanceTransformsList.Count; i++)
		{
			Transform transform = instanceTransformsList[i];
			Vector3 position = camera.WorldToViewportPoint(transform.position);
			position.x = Mathf.Clamp(position.x, boundaryUVSize, 1f - boundaryUVSize);
			position.y = Mathf.Clamp(position.y, boundaryUVSize, 1f - boundaryUVSize);
			transform.position = camera.ViewportToWorldPoint(position);
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

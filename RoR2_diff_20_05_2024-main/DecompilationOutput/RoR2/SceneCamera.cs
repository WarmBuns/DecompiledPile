using UnityEngine;
using UnityEngine.Rendering;

namespace RoR2;

[RequireComponent(typeof(Camera))]
public class SceneCamera : MonoBehaviour
{
	public delegate void SceneCameraDelegate(SceneCamera sceneCamera);

	public static Camera _camera;

	public static OpaqueSortMode sortMode = OpaqueSortMode.NoDistanceSort;

	public Camera camera { get; private set; }

	public CameraRigController cameraRigController { get; private set; }

	public static event SceneCameraDelegate onSceneCameraPreCull;

	public static event SceneCameraDelegate onSceneCameraPreRender;

	public static event SceneCameraDelegate onSceneCameraPostRender;

	private void Awake()
	{
		camera = GetComponent<Camera>();
		cameraRigController = GetComponentInParent<CameraRigController>();
		camera.eventMask = ~camera.cullingMask;
		_camera = camera;
		camera.useOcclusionCulling = false;
	}

	private void OnPreCull()
	{
		if (SceneCamera.onSceneCameraPreCull != null)
		{
			SceneCamera.onSceneCameraPreCull(this);
		}
	}

	private void OnPreRender()
	{
		if (SceneCamera.onSceneCameraPreRender != null)
		{
			camera.opaqueSortMode = sortMode;
			SceneCamera.onSceneCameraPreRender(this);
		}
	}

	private void OnPostRender()
	{
		if (SceneCamera.onSceneCameraPostRender != null)
		{
			SceneCamera.onSceneCameraPostRender(this);
		}
	}

	[ConCommand(commandName = "toggleculling", flags = ConVarFlags.None, helpText = "togggle occlusion culling.")]
	private static void CCToggleCulling(ConCommandArgs args)
	{
		_camera.useOcclusionCulling = !_camera.useOcclusionCulling;
	}

	[ConCommand(commandName = "cycleSortMode", flags = ConVarFlags.None, helpText = "togggle occlusion culling.")]
	private static void CCCycleSortMode(ConCommandArgs args)
	{
		sortMode = (OpaqueSortMode)((int)(sortMode + 1) % 3);
	}
}

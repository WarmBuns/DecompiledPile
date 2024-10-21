using UnityEngine;
using UnityEngine.Rendering;

namespace RoR2.PostProcessing;

[RequireComponent(typeof(Camera))]
public class VisionLimitEffect : MonoBehaviour
{
	private static class ShaderParamsIDs
	{
		public static int origin = Shader.PropertyToID("_Origin");

		public static int rangeStart = Shader.PropertyToID("_RangeStart");

		public static int rangeEnd = Shader.PropertyToID("_RangeEnd");

		public static int color = Shader.PropertyToID("_Color");
	}

	public CameraRigController cameraRigController;

	private Camera camera;

	private float desiredVisionDistance = float.PositiveInfinity;

	private CommandBuffer commandBuffer;

	private Material material;

	private Vector3 lastKnownTargetPosition;

	private float currentAlpha;

	private float alphaVelocity;

	private float currentVisionDistance;

	private float currentVisionDistanceVelocity;

	private void Awake()
	{
		camera = GetComponent<Camera>();
		commandBuffer = new CommandBuffer();
		commandBuffer.name = "VisionLimitEffect";
		Shader shader = LegacyShaderAPI.Find("Hopoo Games/Internal/VisionLimit");
		material = new Material(shader);
		material.name = "VisionLimitEffectMaterial";
	}

	private void OnDestroy()
	{
		DestroyTemporaryAsset(material);
		commandBuffer = null;
		camera = null;
	}

	private void OnEnable()
	{
		camera.AddCommandBuffer(CameraEvent.AfterImageEffectsOpaque, commandBuffer);
	}

	private void OnDisable()
	{
		camera.RemoveCommandBuffer(CameraEvent.AfterImageEffectsOpaque, commandBuffer);
	}

	private void OnPreCull()
	{
		UpdateCommandBuffer();
	}

	private void LateUpdate()
	{
		Transform transform = (cameraRigController.target ? cameraRigController.target.transform : null);
		CharacterBody targetBody = cameraRigController.targetBody;
		if ((bool)transform)
		{
			lastKnownTargetPosition = transform.position;
		}
		desiredVisionDistance = (targetBody ? targetBody.visionDistance : float.PositiveInfinity);
		float target = 0f;
		float target2 = 4000f;
		if (desiredVisionDistance != float.PositiveInfinity)
		{
			target = 1f;
			target2 = desiredVisionDistance;
		}
		currentAlpha = Mathf.SmoothDamp(currentAlpha, target, ref alphaVelocity, 0.2f, float.PositiveInfinity, Time.deltaTime);
		currentVisionDistance = Mathf.SmoothDamp(currentVisionDistance, target2, ref currentVisionDistanceVelocity, 0.2f, float.PositiveInfinity, Time.deltaTime);
	}

	private void UpdateCommandBuffer()
	{
		commandBuffer.Clear();
		if (!(currentAlpha <= 0f))
		{
			float num = Mathf.Max(0f, currentVisionDistance * 0.5f);
			float value = Mathf.Max(num + 0.01f, currentVisionDistance);
			material.SetVector(ShaderParamsIDs.origin, lastKnownTargetPosition);
			material.SetFloat(ShaderParamsIDs.rangeStart, num);
			material.SetFloat(ShaderParamsIDs.rangeEnd, value);
			material.SetColor(ShaderParamsIDs.color, new Color(0f, 0f, 0f, currentAlpha));
			commandBuffer.Blit(null, BuiltinRenderTextureType.CurrentActive, material);
		}
	}

	private void DestroyTemporaryAsset(Object temporaryAsset)
	{
		if (Application.isPlaying)
		{
			Object.Destroy(temporaryAsset);
		}
		else
		{
			Object.DestroyImmediate(temporaryAsset);
		}
	}
}

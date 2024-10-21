using RoR2.ConVar;
using UnityEngine;

namespace RoR2;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraResolutionScaler : MonoBehaviour
{
	private class ResolutionScaleConVar : BaseConVar
	{
		private static ResolutionScaleConVar instance = new ResolutionScaleConVar("resolution_scale", ConVarFlags.Archive, null, "Resolution scale. Currently nonfunctional.");

		private ResolutionScaleConVar(string name, ConVarFlags flags, string defaultValue, string helpText)
			: base(name, flags, defaultValue, helpText)
		{
		}

		public override void SetString(string newValue)
		{
			TextSerialization.TryParseInvariant(newValue, out float _);
		}

		public override string GetString()
		{
			return TextSerialization.ToStringInvariant(resolutionScale);
		}
	}

	private RenderTexture oldRenderTexture;

	private static float resolutionScale = 1f;

	private RenderTexture scalingRenderTexture;

	public Camera camera { get; private set; }

	private void Awake()
	{
		camera = GetComponent<Camera>();
	}

	private void OnPreRender()
	{
		ApplyScalingRenderTexture();
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (!scalingRenderTexture)
		{
			Graphics.Blit(source, destination);
			return;
		}
		camera.targetTexture = oldRenderTexture;
		Graphics.Blit(source, oldRenderTexture);
		oldRenderTexture = null;
	}

	private static void SetResolutionScale(float newResolutionScale)
	{
		if (resolutionScale != newResolutionScale)
		{
			resolutionScale = newResolutionScale;
		}
	}

	private void ApplyScalingRenderTexture()
	{
		oldRenderTexture = camera.targetTexture;
		bool flag = resolutionScale != 1f;
		camera.targetTexture = null;
		Rect pixelRect = camera.pixelRect;
		int num = Mathf.FloorToInt(pixelRect.width * resolutionScale);
		int num2 = Mathf.FloorToInt(pixelRect.height * resolutionScale);
		if ((bool)scalingRenderTexture && (scalingRenderTexture.width != num || scalingRenderTexture.height != num2))
		{
			Object.Destroy(scalingRenderTexture);
			scalingRenderTexture = null;
		}
		if (flag != (bool)scalingRenderTexture)
		{
			if (flag)
			{
				scalingRenderTexture = new RenderTexture(num, num2, 24);
				scalingRenderTexture.autoGenerateMips = false;
				scalingRenderTexture.filterMode = (((double)resolutionScale > 1.0) ? FilterMode.Bilinear : FilterMode.Point);
			}
			else
			{
				Object.Destroy(scalingRenderTexture);
				scalingRenderTexture = null;
			}
		}
		if (flag)
		{
			camera.targetTexture = scalingRenderTexture;
		}
	}

	private void OnDestroy()
	{
		if ((bool)scalingRenderTexture)
		{
			Object.Destroy(scalingRenderTexture);
			scalingRenderTexture = null;
		}
	}
}

using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[PostProcess(typeof(SobelRainRenderer), PostProcessEvent.BeforeTransparent, "PostProcess/SobelRain", true)]
[Serializable]
public sealed class SobelRain : PostProcessEffectSettings
{
	[Range(0f, 100f)]
	[Tooltip("The intensity of the rain.")]
	public FloatParameter rainIntensity = new FloatParameter
	{
		value = 0.5f
	};

	[Range(0f, 10f)]
	[Tooltip("The falloff of the outline. Higher values means it relies less on the sobel.")]
	public FloatParameter outlineScale = new FloatParameter
	{
		value = 1f
	};

	[Range(0f, 1f)]
	[Tooltip("The density of rain.")]
	public FloatParameter rainDensity = new FloatParameter
	{
		value = 1f
	};

	public TextureParameter rainTexture = new TextureParameter
	{
		value = null
	};

	public ColorParameter rainColor = new ColorParameter
	{
		value = Color.white
	};
}

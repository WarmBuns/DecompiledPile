using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[PostProcess(typeof(SobelOutlineRenderer), PostProcessEvent.BeforeTransparent, "PostProcess/SobelOutline", true)]
[Serializable]
public sealed class SobelOutline : PostProcessEffectSettings
{
	[Range(0f, 5f)]
	[Tooltip("The intensity of the outline.")]
	public FloatParameter outlineIntensity = new FloatParameter
	{
		value = 0.5f
	};

	[Range(0f, 10f)]
	[Tooltip("The falloff of the outline.")]
	public FloatParameter outlineScale = new FloatParameter
	{
		value = 1f
	};
}

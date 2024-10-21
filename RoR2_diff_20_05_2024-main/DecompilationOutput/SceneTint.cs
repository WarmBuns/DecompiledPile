using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[PostProcess(typeof(SceneTintRenderer), PostProcessEvent.BeforeTransparent, "PostProcess/SceneTint", true)]
[Serializable]
public sealed class SceneTint : PostProcessEffectSettings
{
	[Range(0f, 1f)]
	[Tooltip("The intensity of the tint.")]
	public FloatParameter tintIntensity = new FloatParameter
	{
		value = 0.5f
	};
}

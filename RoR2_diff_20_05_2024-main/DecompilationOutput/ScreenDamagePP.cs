using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[PostProcess(typeof(ScreenDamagePPRenderer), PostProcessEvent.BeforeStack, "PostProcess/ScreenDamage", true)]
[Serializable]
public sealed class ScreenDamagePP : PostProcessEffectSettings
{
	[Tooltip("Screen tint color")]
	public ColorParameter VignetteTint = new ColorParameter
	{
		value = new Color(0f, 0.5709429f, 0.5955881f)
	};

	[Tooltip("Distortion normal map")]
	public TextureParameter OffsetMap = new TextureParameter();

	[Tooltip("Distortion strength")]
	public FloatParameter DistortionStrength = new FloatParameter();

	[Tooltip("Desaturation strength")]
	public FloatParameter DesaturationStrength = new FloatParameter();

	[Tooltip("Tint strength")]
	public FloatParameter TintStrength = new FloatParameter();
}

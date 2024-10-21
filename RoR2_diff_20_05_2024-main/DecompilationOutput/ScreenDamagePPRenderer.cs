using RoR2;
using RoR2.PostProcessing;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public sealed class ScreenDamagePPRenderer : PostProcessEffectRenderer<ScreenDamagePP>
{
	private static readonly int NormalMap = Shader.PropertyToID("_NormalMap");

	private static readonly int Tint = Shader.PropertyToID("_Tint");

	private static readonly int DistortionStrength = Shader.PropertyToID("_DistortionStrength");

	private static readonly int DesaturationStrength = Shader.PropertyToID("_DesaturationStrength");

	private static readonly int TintStrength = Shader.PropertyToID("_TintStrength");

	private static readonly int CombinationType = Shader.PropertyToID("_CombinationType");

	private Shader shader;

	private static bool propsSet;

	private static bool usingTemporaryProps;

	private static int temporaryPropsSetExpirationFrameStamp;

	public static ColorParameter temporaryVignetteTint;

	public static TextureParameter temporaryOffsetMap;

	public static bool temporaryCombineAdditively;

	public static ScreenDamageCalculator temporaryPropsCalculator;

	private int temporaryPropsExpirationFrames = 2;

	public override void Init()
	{
		base.Init();
		shader = LegacyShaderAPI.Find("Shaders/PostProcess/HGScreenDamage");
		propsSet = false;
	}

	public static void ExtendTemporaryProps()
	{
		temporaryPropsSetExpirationFrameStamp++;
	}

	public static void SetTemporaryProps(ScreenDamageCalculator damageCalculator, ColorParameter tintColor, TextureParameter offsetMap, bool combineAdditively)
	{
		usingTemporaryProps = true;
		temporaryPropsCalculator = damageCalculator;
		temporaryVignetteTint = tintColor;
		temporaryOffsetMap = offsetMap;
		temporaryCombineAdditively = combineAdditively;
		temporaryPropsSetExpirationFrameStamp = int.MaxValue;
		propsSet = false;
	}

	public static void ClearTemporaryProps(ScreenDamageCalculator damageCalculator)
	{
		if (damageCalculator == temporaryPropsCalculator)
		{
			temporaryOffsetMap = null;
			temporaryVignetteTint = null;
			usingTemporaryProps = false;
			temporaryCombineAdditively = false;
			temporaryPropsSetExpirationFrameStamp = int.MaxValue;
			propsSet = false;
		}
	}

	private void HandlePropsSet(PropertySheet sheet, int frameCount)
	{
		propsSet = true;
		if (usingTemporaryProps && temporaryOffsetMap != null)
		{
			sheet.properties.SetTexture(NormalMap, temporaryOffsetMap);
		}
		else
		{
			sheet.properties.SetTexture(NormalMap, base.settings.OffsetMap);
		}
		if (usingTemporaryProps && temporaryVignetteTint != null)
		{
			sheet.properties.SetColor(Tint, temporaryVignetteTint);
		}
		else
		{
			sheet.properties.SetColor(Tint, base.settings.VignetteTint);
		}
		if (usingTemporaryProps && temporaryCombineAdditively)
		{
			sheet.properties.SetFloat(CombinationType, 1f);
		}
		else
		{
			sheet.properties.SetFloat(CombinationType, -1f);
		}
		if (usingTemporaryProps && (temporaryOffsetMap != null || temporaryVignetteTint != null))
		{
			temporaryPropsSetExpirationFrameStamp = frameCount + temporaryPropsExpirationFrames;
		}
		else
		{
			temporaryPropsSetExpirationFrameStamp = int.MaxValue;
		}
	}

	public override void Render(PostProcessRenderContext context)
	{
		PropertySheet propertySheet = context.propertySheets.Get(shader);
		int frameCount = Time.frameCount;
		if (usingTemporaryProps && propsSet && frameCount > temporaryPropsSetExpirationFrameStamp)
		{
			ClearTemporaryProps(temporaryPropsCalculator);
		}
		if (!propsSet)
		{
			HandlePropsSet(propertySheet, frameCount);
		}
		propertySheet.properties.SetFloat(DistortionStrength, base.settings.DistortionStrength);
		propertySheet.properties.SetFloat(DesaturationStrength, base.settings.DesaturationStrength);
		propertySheet.properties.SetFloat(TintStrength, base.settings.TintStrength);
		context.command.BlitFullscreenTriangle(context.source, context.destination, propertySheet, 0);
	}
}

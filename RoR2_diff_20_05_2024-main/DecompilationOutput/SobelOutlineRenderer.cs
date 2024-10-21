using RoR2;
using UnityEngine.Rendering.PostProcessing;

public sealed class SobelOutlineRenderer : PostProcessEffectRenderer<SobelOutline>
{
	public override void Render(PostProcessRenderContext context)
	{
		PropertySheet propertySheet = context.propertySheets.Get(LegacyShaderAPI.Find("Hidden/PostProcess/SobelOutline"));
		propertySheet.properties.SetFloat("_OutlineIntensity", base.settings.outlineIntensity);
		propertySheet.properties.SetFloat("_OutlineScale", base.settings.outlineScale);
		context.command.BlitFullscreenTriangle(context.source, context.destination, propertySheet, 0);
	}
}

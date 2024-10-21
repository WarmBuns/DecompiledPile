using RoR2;
using UnityEngine.Rendering.PostProcessing;

public sealed class SceneTintRenderer : PostProcessEffectRenderer<SceneTint>
{
	public override void Render(PostProcessRenderContext context)
	{
		PropertySheet propertySheet = context.propertySheets.Get(LegacyShaderAPI.Find("Hidden/PostProcess/SceneTint"));
		propertySheet.properties.SetFloat("_TintIntensity", base.settings.tintIntensity);
		context.command.BlitFullscreenTriangle(context.source, context.destination, propertySheet, 0);
	}
}

using UnityEngine;

namespace RoR2.PostProcessing;

[ExecuteInEditMode]
public class HopooPostProcess : MonoBehaviour
{
	public Material mat;

	private void Start()
	{
	}

	[ImageEffectOpaque]
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		Graphics.Blit(source, destination, mat);
	}
}

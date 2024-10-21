using UnityEngine;

namespace RoR2;

[ExecuteInEditMode]
public class RainPostProcess : MonoBehaviour
{
	public Material mat;

	private RenderTexture renderTex;

	private void Start()
	{
		GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
	}

	private void Update()
	{
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		Graphics.Blit(source, destination, mat);
	}
}

using UnityEngine;

[ExecuteInEditMode]
public class GlobalShaderTextures : MonoBehaviour
{
	public Texture warpRampTexture;

	public string warpRampShaderVariableName;

	public Texture eliteRampTexture;

	public string eliteRampShaderVariableName;

	public Texture snowMicrofacetTexture;

	public string snowMicrofacetNoiseVariableName;

	private void OnValidate()
	{
		Shader.SetGlobalTexture(warpRampShaderVariableName, warpRampTexture);
		Shader.SetGlobalTexture(eliteRampShaderVariableName, eliteRampTexture);
		Shader.SetGlobalTexture(snowMicrofacetNoiseVariableName, snowMicrofacetTexture);
	}

	private void Start()
	{
		Shader.SetGlobalTexture(warpRampShaderVariableName, warpRampTexture);
		Shader.SetGlobalTexture(eliteRampShaderVariableName, eliteRampTexture);
		Shader.SetGlobalTexture(snowMicrofacetNoiseVariableName, snowMicrofacetTexture);
	}
}

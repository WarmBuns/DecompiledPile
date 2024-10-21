using UnityEngine;

namespace RoR2;

public static class CommonShaderProperties
{
	public static readonly int _FlashColor = Shader.PropertyToID("_FlashColor");

	public static readonly int _Fade = Shader.PropertyToID("_Fade");

	public static readonly int _EliteIndex = Shader.PropertyToID("_EliteIndex");

	public static readonly int _LimbPrimeMask = Shader.PropertyToID("_LimbPrimeMask");

	public static readonly int _ExternalAlpha = Shader.PropertyToID("_ExternalAlpha");
}

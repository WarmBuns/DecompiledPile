using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/SurfaceDef")]
public class SurfaceDef : ScriptableObject
{
	[HideInInspector]
	public SurfaceDefIndex surfaceDefIndex = SurfaceDefIndex.Invalid;

	public Color approximateColor;

	public GameObject impactEffectPrefab;

	public GameObject footstepEffectPrefab;

	public string impactSoundString;

	public string materialSwitchString;

	public bool isSlippery;

	public bool isLava;

	public float damage;

	public float speedBoost;

	private void OnValidate()
	{
		if (!Application.isPlaying && surfaceDefIndex != SurfaceDefIndex.Invalid)
		{
			surfaceDefIndex = SurfaceDefIndex.Invalid;
		}
	}
}

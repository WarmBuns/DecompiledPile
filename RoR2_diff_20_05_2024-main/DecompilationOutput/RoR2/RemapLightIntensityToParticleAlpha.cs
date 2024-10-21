using UnityEngine;

namespace RoR2;

public class RemapLightIntensityToParticleAlpha : MonoBehaviour
{
	public Light light;

	public ParticleSystem particleSystem;

	public float lowerLightIntensity;

	public float upperLightIntensity = 1f;

	public float lowerParticleAlpha;

	public float upperParticleAlpha = 1f;

	private void LateUpdate()
	{
		ParticleSystem.MainModule main = particleSystem.main;
		ParticleSystem.MinMaxGradient startColor = main.startColor;
		Color color = startColor.color;
		color.a = Util.Remap(light.intensity, lowerLightIntensity, upperLightIntensity, lowerParticleAlpha, upperParticleAlpha);
		startColor.color = color;
		main.startColor = startColor;
	}
}

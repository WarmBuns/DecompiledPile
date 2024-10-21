using UnityEngine;

namespace RoR2;

public class ReplaceLightColorsInChildren : MonoBehaviour
{
	public Color newLightColor;

	public float intensityMultiplier;

	public Material newParticleSystemMaterial;

	private void Awake()
	{
		Light[] componentsInChildren = GetComponentsInChildren<Light>();
		foreach (Light obj in componentsInChildren)
		{
			obj.color = newLightColor;
			obj.intensity *= intensityMultiplier;
		}
		if (!newParticleSystemMaterial)
		{
			return;
		}
		ParticleSystem[] componentsInChildren2 = GetComponentsInChildren<ParticleSystem>();
		for (int i = 0; i < componentsInChildren2.Length; i++)
		{
			ParticleSystemRenderer component = componentsInChildren2[i].GetComponent<ParticleSystemRenderer>();
			if ((bool)component)
			{
				component.material = newParticleSystemMaterial;
			}
		}
	}

	private void Update()
	{
	}
}

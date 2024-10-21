using System;
using UnityEngine;

namespace RoR2;

public class OmniEffect : MonoBehaviour
{
	[Serializable]
	public class OmniEffectElement
	{
		public string name;

		public ParticleSystem particleSystem;

		public bool particleSystemEmitParticles;

		public Material particleSystemOverrideMaterial;

		public float maximumValidRadius = float.PositiveInfinity;

		public float bonusEmissionPerBonusRadius;

		public void ProcessEffectElement(float radius, float minimumValidRadius)
		{
			if (!particleSystem)
			{
				return;
			}
			if ((bool)particleSystemOverrideMaterial)
			{
				ParticleSystemRenderer component = particleSystem.GetComponent<ParticleSystemRenderer>();
				if ((bool)particleSystemOverrideMaterial)
				{
					component.material = particleSystemOverrideMaterial;
				}
			}
			ParticleSystem.EmissionModule emission = particleSystem.emission;
			if (emission.burstCount > 0)
			{
				int num = Mathf.Min(emission.GetBurst(0).maxCount + (int)((radius - minimumValidRadius) * bonusEmissionPerBonusRadius), particleSystem.main.maxParticles);
				emission.SetBurst(0, new ParticleSystem.Burst(0f, num));
			}
			if (particleSystemEmitParticles)
			{
				particleSystem.gameObject.SetActive(value: true);
			}
			else
			{
				particleSystem.gameObject.SetActive(value: false);
			}
		}
	}

	[Serializable]
	public class OmniEffectGroup
	{
		public string name;

		public OmniEffectElement[] omniEffectElements;
	}

	public OmniEffectGroup[] omniEffectGroups;

	private float radius;

	private void Start()
	{
		radius = base.transform.localScale.x;
		OmniEffectGroup[] array = omniEffectGroups;
		foreach (OmniEffectGroup omniEffectGroup in array)
		{
			float minimumValidRadius = 0f;
			for (int j = 0; j < omniEffectGroup.omniEffectElements.Length; j++)
			{
				OmniEffectElement omniEffectElement = omniEffectGroup.omniEffectElements[j];
				if (omniEffectElement.maximumValidRadius >= radius)
				{
					omniEffectElement.ProcessEffectElement(radius, minimumValidRadius);
					break;
				}
				minimumValidRadius = omniEffectElement.maximumValidRadius;
			}
		}
	}
}

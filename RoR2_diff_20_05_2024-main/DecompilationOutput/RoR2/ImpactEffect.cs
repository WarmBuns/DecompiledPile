using System;
using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(EffectComponent))]
internal class ImpactEffect : MonoBehaviour
{
	public ParticleSystem[] particleSystems = Array.Empty<ParticleSystem>();

	private void Start()
	{
		EffectComponent component = GetComponent<EffectComponent>();
		Color color = ((component.effectData != null) ? ((Color)component.effectData.color) : Color.white);
		for (int i = 0; i < particleSystems.Length; i++)
		{
			ParticleSystem.MainModule main = particleSystems[i].main;
			main.startColor = color;
			particleSystems[i].Play();
		}
	}
}

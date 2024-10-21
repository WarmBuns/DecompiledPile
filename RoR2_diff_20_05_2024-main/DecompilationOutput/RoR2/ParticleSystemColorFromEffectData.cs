using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(EffectComponent))]
public class ParticleSystemColorFromEffectData : MonoBehaviour
{
	public ParticleSystem[] particleSystems;

	public EffectComponent effectComponent;

	private void Start()
	{
		Color color = effectComponent.effectData.color;
		for (int i = 0; i < particleSystems.Length; i++)
		{
			ParticleSystem.MainModule main = particleSystems[i].main;
			main.startColor = color;
			particleSystems[i].Clear();
			particleSystems[i].Play();
		}
	}
}

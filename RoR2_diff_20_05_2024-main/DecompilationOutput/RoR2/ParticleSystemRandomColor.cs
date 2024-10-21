using UnityEngine;

namespace RoR2;

public class ParticleSystemRandomColor : MonoBehaviour
{
	public Color[] colors;

	public ParticleSystem[] particleSystems;

	private void Awake()
	{
		if (colors.Length != 0)
		{
			Color color = colors[Random.Range(0, colors.Length)];
			for (int i = 0; i < particleSystems.Length; i++)
			{
				ParticleSystem.MainModule main = particleSystems[i].main;
				main.startColor = color;
			}
		}
	}

	[AssetCheck(typeof(ParticleSystemRandomColor))]
	private static void CheckParticleSystemRandomColor(AssetCheckArgs args)
	{
		ParticleSystemRandomColor particleSystemRandomColor = (ParticleSystemRandomColor)args.asset;
		for (int i = 0; i < particleSystemRandomColor.particleSystems.Length; i++)
		{
			if (!particleSystemRandomColor.particleSystems[i])
			{
				args.LogErrorFormat(args.asset, "Null particle system in slot {0}", i);
			}
		}
	}
}

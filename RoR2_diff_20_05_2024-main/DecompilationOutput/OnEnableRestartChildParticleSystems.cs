using System.Collections.Generic;
using UnityEngine;

public class OnEnableRestartChildParticleSystems : MonoBehaviour
{
	[SerializeField]
	private List<ParticleSystem> particleSystemList = new List<ParticleSystem>();

	private void OnEnable()
	{
		if (particleSystemList == null || particleSystemList.Count <= 0)
		{
			return;
		}
		foreach (ParticleSystem particleSystem in particleSystemList)
		{
			if (!particleSystem.isPlaying)
			{
				particleSystem.Play();
			}
		}
	}

	private void OnDisable()
	{
		if (particleSystemList == null || particleSystemList.Count <= 0)
		{
			return;
		}
		foreach (ParticleSystem particleSystem in particleSystemList)
		{
			particleSystem.Stop();
			particleSystem.Clear();
		}
	}
}

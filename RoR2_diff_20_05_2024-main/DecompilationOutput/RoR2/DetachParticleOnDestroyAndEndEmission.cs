using UnityEngine;

namespace RoR2;

public class DetachParticleOnDestroyAndEndEmission : MonoBehaviour
{
	public ParticleSystem particleSystem;

	private void OnDisable()
	{
		if ((bool)particleSystem)
		{
			ParticleSystem.EmissionModule emission = particleSystem.emission;
			emission.enabled = false;
			ParticleSystem.MainModule main = particleSystem.main;
			main.stopAction = ParticleSystemStopAction.Destroy;
			particleSystem.Stop();
			particleSystem.transform.SetParent(null);
			if (particleSystem.isStopped)
			{
				Object.Destroy(particleSystem.gameObject);
			}
		}
	}
}

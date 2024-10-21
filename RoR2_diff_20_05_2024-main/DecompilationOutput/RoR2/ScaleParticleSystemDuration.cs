using UnityEngine;

namespace RoR2;

public class ScaleParticleSystemDuration : MonoBehaviour
{
	public float initialDuration = 1f;

	private float _newDuration = 1f;

	public ParticleSystem[] particleSystems;

	public float newDuration
	{
		get
		{
			return _newDuration;
		}
		set
		{
			if (_newDuration != value)
			{
				_newDuration = value;
				UpdateParticleDurations();
			}
		}
	}

	private void Start()
	{
		UpdateParticleDurations();
	}

	public void UpdateDuration()
	{
		float simulationSpeed = initialDuration / _newDuration;
		for (int i = 0; i < particleSystems.Length; i++)
		{
			ParticleSystem particleSystem = particleSystems[i];
			if ((bool)particleSystem)
			{
				ParticleSystem.MainModule main = particleSystem.main;
				particleSystem.Stop();
				particleSystem.Clear();
				main.duration = _newDuration;
				main.simulationSpeed = simulationSpeed;
				particleSystem.Play();
			}
		}
	}

	private void UpdateParticleDurations()
	{
		float simulationSpeed = initialDuration / _newDuration;
		for (int i = 0; i < particleSystems.Length; i++)
		{
			ParticleSystem particleSystem = particleSystems[i];
			if ((bool)particleSystem)
			{
				ParticleSystem.MainModule main = particleSystem.main;
				main.simulationSpeed = simulationSpeed;
			}
		}
	}
}

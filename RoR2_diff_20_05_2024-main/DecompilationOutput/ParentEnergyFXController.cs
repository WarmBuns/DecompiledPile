using System.Collections.Generic;
using UnityEngine;

public class ParentEnergyFXController : MonoBehaviour
{
	public List<ParticleSystem> energyFXParticles = new List<ParticleSystem>();

	public ParticleSystem loomingPresenceParticles;

	private void Start()
	{
	}

	private void FixedUpdate()
	{
	}

	public void TurnOffFX()
	{
		for (int i = 0; i < energyFXParticles.Count; i++)
		{
			ParticleSystem.EmissionModule emission = energyFXParticles[i].emission;
			emission.enabled = false;
		}
	}

	public void TurnOnFX()
	{
		for (int i = 0; i < energyFXParticles.Count; i++)
		{
			ParticleSystem.EmissionModule emission = energyFXParticles[i].emission;
			emission.enabled = true;
		}
	}

	public void SetLoomingPresenceParticles(bool setTo)
	{
		ParticleSystem.EmissionModule emission = loomingPresenceParticles.emission;
		emission.enabled = setTo;
	}
}

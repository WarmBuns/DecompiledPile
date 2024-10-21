using System.Collections.Generic;
using UnityEngine;

public class LunarWispFXController : MonoBehaviour
{
	public List<ParticleSystem> FXParticles = new List<ParticleSystem>();

	private void Start()
	{
	}

	private void FixedUpdate()
	{
	}

	public void TurnOffFX()
	{
		for (int i = 0; i < FXParticles.Count; i++)
		{
			ParticleSystem.EmissionModule emission = FXParticles[i].emission;
			emission.enabled = false;
		}
	}

	public void TurnOnFX()
	{
		for (int i = 0; i < FXParticles.Count; i++)
		{
			ParticleSystem.EmissionModule emission = FXParticles[i].emission;
			emission.enabled = true;
		}
	}
}

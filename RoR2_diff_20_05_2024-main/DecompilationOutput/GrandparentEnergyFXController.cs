using System.Collections.Generic;
using RoR2;
using UnityEngine;

public class GrandparentEnergyFXController : MonoBehaviour
{
	public List<ParticleSystem> energyFXParticles = new List<ParticleSystem>();

	[HideInInspector]
	public GameObject portalObject;

	private bool isPortalSoundPlaying;

	private void Start()
	{
	}

	private void FixedUpdate()
	{
		if (!(portalObject != null))
		{
			return;
		}
		if (!isPortalSoundPlaying)
		{
			if (portalObject.transform.localScale == Vector3.zero)
			{
				Util.PlaySound("Play_grandparent_portal_loop", portalObject);
				isPortalSoundPlaying = false;
			}
		}
		else if (portalObject.transform.localScale != Vector3.zero)
		{
			Util.PlaySound("Stop_grandparent_portal_loop", portalObject);
			isPortalSoundPlaying = true;
		}
	}

	public void TurnOffFX()
	{
		for (int i = 0; i < energyFXParticles.Count; i++)
		{
			ParticleSystem.EmissionModule emission = energyFXParticles[i].emission;
			emission.enabled = false;
		}
		if (portalObject != null)
		{
			ParticleSystem componentInChildren = portalObject.GetComponentInChildren<ParticleSystem>();
			if (componentInChildren != null)
			{
				ParticleSystem.EmissionModule emission2 = componentInChildren.emission;
				emission2.enabled = false;
			}
		}
	}

	public void TurnOnFX()
	{
		for (int i = 0; i < energyFXParticles.Count; i++)
		{
			ParticleSystem.EmissionModule emission = energyFXParticles[i].emission;
			emission.enabled = true;
		}
		if (portalObject != null)
		{
			ParticleSystem componentInChildren = portalObject.GetComponentInChildren<ParticleSystem>();
			if (componentInChildren != null)
			{
				ParticleSystem.EmissionModule emission2 = componentInChildren.emission;
				emission2.enabled = true;
			}
		}
	}
}

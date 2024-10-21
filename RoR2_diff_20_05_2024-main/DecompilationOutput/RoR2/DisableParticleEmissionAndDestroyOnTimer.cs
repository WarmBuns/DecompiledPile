using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class DisableParticleEmissionAndDestroyOnTimer : MonoBehaviour
{
	[Tooltip("How long to wait before destroying or repooling the object")]
	public float waitDuration = 3f;

	[Tooltip("Particle systems to be stopped.")]
	public List<ParticleSystem> particleSystems;

	public bool StartParticleEmissionOnEnable = true;

	private bool countdownStarted;

	private float timeRemaining;

	private EffectManagerHelper efh;

	private void Awake()
	{
		efh = GetComponent<EffectManagerHelper>();
	}

	public void DisableParticlesStartTimer()
	{
		countdownStarted = true;
		timeRemaining = waitDuration;
		foreach (ParticleSystem particleSystem in particleSystems)
		{
			particleSystem.Stop();
		}
	}

	private void Update()
	{
		if (countdownStarted)
		{
			timeRemaining -= Time.deltaTime;
			if (timeRemaining < 0f)
			{
				DestroyOrRepoolObject();
			}
		}
	}

	private void DestroyOrRepoolObject()
	{
		if ((bool)efh && efh.OwningPool != null)
		{
			efh.OwningPool.ReturnObject(efh);
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}

	private void OnEnable()
	{
		countdownStarted = false;
		if (!StartParticleEmissionOnEnable)
		{
			return;
		}
		foreach (ParticleSystem particleSystem in particleSystems)
		{
			particleSystem.Play();
		}
	}
}

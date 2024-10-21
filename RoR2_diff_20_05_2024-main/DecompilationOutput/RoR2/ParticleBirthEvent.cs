using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public class ParticleBirthEvent : MonoBehaviour
{
	public ParticleSystem particleSystemToTrack;

	public bool OnServerOnly;

	[Tooltip("Displays a message whenever a new particle is born")]
	public bool debugMessages;

	[Space(20f)]
	public UnityEvent onParticleBirth;

	private ParticleSystem.Particle[] particleArray;

	private void Awake()
	{
		if (particleSystemToTrack == null)
		{
			particleSystemToTrack = GetComponent<ParticleSystem>();
		}
		if (particleSystemToTrack != null)
		{
			particleArray = new ParticleSystem.Particle[particleSystemToTrack.main.maxParticles];
		}
	}

	private void LateUpdate()
	{
		if (particleSystemToTrack == null || (OnServerOnly && !NetworkServer.active))
		{
			base.enabled = false;
		}
		else
		{
			if (!particleSystemToTrack.isEmitting)
			{
				return;
			}
			int particles = particleSystemToTrack.GetParticles(particleArray);
			for (int i = 0; i < particles; i++)
			{
				ParticleSystem.Particle particle = particleArray[i];
				if (Mathf.Approximately(particle.startLifetime, particle.remainingLifetime))
				{
					FireEvent();
					break;
				}
			}
		}
	}

	private void FireEvent()
	{
		_ = debugMessages;
		onParticleBirth?.Invoke();
	}
}

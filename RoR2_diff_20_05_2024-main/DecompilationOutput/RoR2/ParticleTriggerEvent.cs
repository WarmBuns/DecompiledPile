using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public class ParticleTriggerEvent : MonoBehaviour
{
	public ParticleSystem particleSystemToTrack;

	public bool OnServerOnly;

	public bool killParticleOnEventTrigger;

	[Tooltip("Displays a message whenever a new particle is born")]
	public bool debugMessages;

	[Space(20f)]
	public ParticleSystemTriggerEventType triggerType;

	[Space(20f)]
	[Tooltip("The trigger will not fire if it's fired within this many seconds ago. Leave at -1 for 'fire every frame if need be.'")]
	public float triggerCooldown = -1f;

	[Space(20f)]
	public UnityEvent onParticleTrigger;

	private List<ParticleSystem.Particle> particleList;

	private float lastTriggerTimestamp;

	private void Awake()
	{
		if (particleSystemToTrack == null)
		{
			particleSystemToTrack = GetComponent<ParticleSystem>();
		}
		if (particleSystemToTrack != null)
		{
			particleList = new List<ParticleSystem.Particle>(particleSystemToTrack.main.maxParticles);
		}
		if (particleSystemToTrack == null || (OnServerOnly && !NetworkServer.active))
		{
			base.enabled = false;
		}
	}

	private void OnParticleTrigger()
	{
		float time = Time.time;
		if (particleSystemToTrack.GetTriggerParticles(triggerType, particleList) > 0)
		{
			FireEvent(time);
			if (killParticleOnEventTrigger)
			{
				KillParticles(triggerType);
			}
		}
	}

	private void KillParticles(ParticleSystemTriggerEventType trigType)
	{
		int count = particleList.Count;
		for (int i = 0; i < count; i++)
		{
			ParticleSystem.Particle value = particleList[i];
			value.remainingLifetime = -1f;
			particleList[i] = value;
		}
		_ = debugMessages;
		particleSystemToTrack.SetTriggerParticles(trigType, particleList);
	}

	private void FireEvent(float currentTime)
	{
		if (triggerCooldown == -1f || !(currentTime - lastTriggerTimestamp < triggerCooldown))
		{
			_ = debugMessages;
			lastTriggerTimestamp = currentTime;
			onParticleTrigger?.Invoke();
		}
	}
}

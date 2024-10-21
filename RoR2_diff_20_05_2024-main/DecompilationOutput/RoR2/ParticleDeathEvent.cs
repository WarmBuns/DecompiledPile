using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public class ParticleDeathEvent : MonoBehaviour
{
	public ParticleSystem particleSystemToTrack;

	public bool OnServerOnly;

	[Tooltip("Displays a message whenever a new particle is born")]
	public bool debugMessages;

	[Space(20f)]
	[Tooltip("The trigger will not fire if it's fired within this many seconds ago. Leave at -1 for 'fire every frame if need be.'")]
	public float triggerCooldown = -1f;

	[Space(20f)]
	public UnityEvent onParticleTrigger;

	private ParticleSystem.Particle[] particleArray;

	private float lastTriggerTimestamp;

	private float nextEventFireTimestamp = -1f;

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
			return;
		}
		float time = Time.time;
		if (nextEventFireTimestamp > 0f && time > nextEventFireTimestamp)
		{
			HandleFireEvent(time);
		}
		if (particleSystemToTrack.isEmitting)
		{
			HandleEstimateNextParticleDeath(time);
		}
	}

	private void HandleEstimateNextParticleDeath(float currentTime)
	{
		int particles = particleSystemToTrack.GetParticles(particleArray);
		float num = ((nextEventFireTimestamp != -1f) ? (nextEventFireTimestamp - currentTime) : float.MaxValue);
		for (int i = 0; i < particles; i++)
		{
			float remainingLifetime = particleArray[i].remainingLifetime;
			if (remainingLifetime < num)
			{
				num = remainingLifetime;
			}
		}
		if (num != float.MaxValue)
		{
			nextEventFireTimestamp = currentTime + num;
		}
	}

	private void HandleFireEvent(float currentTime)
	{
		nextEventFireTimestamp = -1f;
		if (!(currentTime - lastTriggerTimestamp < triggerCooldown))
		{
			_ = debugMessages;
			lastTriggerTimestamp = currentTime;
			onParticleTrigger?.Invoke();
		}
	}
}

using System.Collections.Generic;
using AK.Wwise;
using UnityEngine;

namespace RoR2;

public class ParticleLifetimeSoundPlayer : MonoBehaviour
{
	private class ParticleLifetimeTracker
	{
		private static int idCounter;

		public bool isActive;

		private bool firedStart;

		private bool firedEnd;

		private float lifetimeTimeStamp;

		private float startTimeTimeStamp;

		private float endTimeTimeStamp;

		private int localId;

		private AK.Wwise.Event startEvent;

		private AK.Wwise.Event endEvent;

		private GameObject soundObject;

		public ParticleLifetimeTracker(GameObject go)
		{
			soundObject = go;
		}

		public void Initialize(float currentTime, float lifeTime, float startTimeOffset, float endTimeOffset, ref AK.Wwise.Event _startEvent, ref AK.Wwise.Event _endEvent, bool debug)
		{
			localId = idCounter++;
			lifetimeTimeStamp = currentTime + lifeTime;
			startTimeTimeStamp = currentTime + startTimeOffset;
			endTimeTimeStamp = lifetimeTimeStamp + endTimeOffset;
			firedStart = false;
			firedEnd = false;
			isActive = true;
			startEvent = _startEvent;
			endEvent = _endEvent;
		}

		public void Update(float currentTime, bool debug)
		{
			if (isActive)
			{
				if (!firedStart && currentTime > startTimeTimeStamp)
				{
					firedStart = true;
					startEvent?.Post(soundObject);
				}
				if (!firedEnd && currentTime > endTimeTimeStamp)
				{
					firedEnd = true;
					endEvent?.Post(soundObject);
				}
				isActive = currentTime < lifetimeTimeStamp;
				_ = !isActive && debug;
			}
		}
	}

	public ParticleSystem particleSystemToTrack;

	[Tooltip("How long after a particle is born should the StartEvent be posted.")]
	public float startTimeOffset;

	[Tooltip("How long before/after a particle dies should the EndEvent be posted.")]
	public float endTimeOffset;

	[Tooltip("Displays a message whenever a new particle is born")]
	public bool debugMessages;

	public AK.Wwise.Event startEvent;

	public AK.Wwise.Event endEvent;

	[Tooltip("How long between particles should we wait before firing the next sound. -1 = no delay")]
	public float soundCooldown = -1f;

	private float lastParticleTrackedTimeStamp;

	private ParticleSystem.Particle[] particleArray;

	[SerializeField]
	[Space(20f)]
	public List<GameObject> soundObjects = new List<GameObject>();

	private List<ParticleLifetimeTracker> particleTrackers = new List<ParticleLifetimeTracker>();

	private void Awake()
	{
		if (particleSystemToTrack == null)
		{
			particleSystemToTrack = GetComponent<ParticleSystem>();
		}
		int count = soundObjects.Count;
		for (int i = 0; i < count; i++)
		{
			particleTrackers.Add(new ParticleLifetimeTracker(soundObjects[i]));
		}
		if (particleSystemToTrack != null)
		{
			particleArray = new ParticleSystem.Particle[particleSystemToTrack.main.maxParticles];
		}
	}

	private void LateUpdate()
	{
		if (particleSystemToTrack == null)
		{
			base.enabled = false;
			return;
		}
		float time = Time.time;
		UpdateExistingParticlePlayers(time);
		if (particleSystemToTrack.isEmitting && !(time - lastParticleTrackedTimeStamp < soundCooldown))
		{
			DiscoverAndTrackNewParticles(time);
		}
	}

	private void UpdateExistingParticlePlayers(float currentTime)
	{
		for (int i = 0; i < particleTrackers.Count; i++)
		{
			particleTrackers[i]?.Update(currentTime, debugMessages);
		}
	}

	private ParticleLifetimeTracker FindAvailableParticleTracker()
	{
		int count = particleTrackers.Count;
		for (int i = 0; i < count; i++)
		{
			if (!particleTrackers[i].isActive)
			{
				return particleTrackers[i];
			}
		}
		return null;
	}

	private void DiscoverAndTrackNewParticles(float currentTime)
	{
		ParticleLifetimeTracker particleLifetimeTracker = FindAvailableParticleTracker();
		if (particleLifetimeTracker == null)
		{
			return;
		}
		int particles = particleSystemToTrack.GetParticles(particleArray);
		for (int i = 0; i < particles; i++)
		{
			ParticleSystem.Particle particle = particleArray[i];
			if (Mathf.Approximately(particle.startLifetime, particle.remainingLifetime))
			{
				lastParticleTrackedTimeStamp = currentTime;
				particleLifetimeTracker.Initialize(currentTime, particle.remainingLifetime, startTimeOffset, endTimeOffset, ref startEvent, ref endEvent, debugMessages);
				break;
			}
		}
	}
}

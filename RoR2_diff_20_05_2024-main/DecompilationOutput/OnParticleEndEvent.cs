using UnityEngine;
using UnityEngine.Events;

public class OnParticleEndEvent : MonoBehaviour
{
	public ParticleSystem particleSystemToTrack;

	public UnityEvent onEnd;

	private bool particleEnded;

	public void Update()
	{
		if ((bool)particleSystemToTrack && !particleSystemToTrack.IsAlive() && !particleEnded)
		{
			particleEnded = true;
			onEnd.Invoke();
		}
	}
}

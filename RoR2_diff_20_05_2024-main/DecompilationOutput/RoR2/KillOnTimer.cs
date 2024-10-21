using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class KillOnTimer : MonoBehaviour
{
	public float duration;

	private float age;

	private HealthComponent health;

	private void Start()
	{
		if (!TryGetComponent<HealthComponent>(out var component))
		{
			Debug.LogWarning(base.gameObject.name + " does not have a health component!  KillOnTimer will not work!!");
		}
		else
		{
			health = component;
		}
	}

	private void Update()
	{
		age += Time.deltaTime;
		if (age > duration)
		{
			if ((bool)health && NetworkServer.active)
			{
				health.Suicide();
			}
			base.enabled = false;
		}
	}
}

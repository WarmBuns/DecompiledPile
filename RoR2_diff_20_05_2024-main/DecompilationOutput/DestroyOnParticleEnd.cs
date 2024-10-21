using RoR2;
using UnityEngine;

public class DestroyOnParticleEnd : MonoBehaviour
{
	[SerializeField]
	[Tooltip("Set this if you want to specify which particle system is tracked. Otherwise it will automatically grab the first one it finds.")]
	private ParticleSystem trackedParticleSystem;

	private EffectManagerHelper efh;

	public void Start()
	{
		if (trackedParticleSystem == null)
		{
			trackedParticleSystem = GetComponentInChildren<ParticleSystem>();
		}
		if (!efh)
		{
			efh = GetComponent<EffectManagerHelper>();
		}
	}

	public void Update()
	{
		if ((bool)trackedParticleSystem && !trackedParticleSystem.IsAlive())
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
	}
}

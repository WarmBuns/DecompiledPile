using UnityEngine;

namespace RoR2.Orbs;

public class DelayedHitOrb : GenericDamageOrb
{
	public float delay;

	public GameObject delayedEffectPrefab;

	public GameObject orbEffect;

	public override void Begin()
	{
		base.Begin();
		base.duration = delay;
	}

	protected override GameObject GetOrbEffect()
	{
		return orbEffect;
	}

	public override void OnArrival()
	{
		if ((bool)target && (bool)target.transform)
		{
			EffectManager.SpawnEffect(delayedEffectPrefab, new EffectData
			{
				origin = target.transform.position
			}, transmit: true);
			base.OnArrival();
		}
	}
}

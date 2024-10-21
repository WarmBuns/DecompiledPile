using UnityEngine;

namespace RoR2.Orbs;

public class HealOrb : Orb
{
	public float healValue;

	public bool scaleOrb = true;

	public float overrideDuration = 0.3f;

	public override void Begin()
	{
		if ((bool)target)
		{
			base.duration = overrideDuration;
			float scale = (scaleOrb ? Mathf.Min(healValue / target.healthComponent.fullHealth, 1f) : 1f);
			EffectData effectData = new EffectData
			{
				scale = scale,
				origin = origin,
				genericFloat = base.duration
			};
			effectData.SetHurtBoxReference(target);
			EffectManager.SpawnEffect(OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/HealthOrbEffect"), effectData, transmit: true);
		}
	}

	public override void OnArrival()
	{
		if ((bool)target)
		{
			HealthComponent healthComponent = target.healthComponent;
			if ((bool)healthComponent)
			{
				healthComponent.Heal(healValue, default(ProcChainMask));
			}
		}
	}
}

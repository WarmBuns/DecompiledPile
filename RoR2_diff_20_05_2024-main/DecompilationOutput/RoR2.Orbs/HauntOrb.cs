using UnityEngine;

namespace RoR2.Orbs;

public class HauntOrb : Orb
{
	public float timeToArrive;

	public float scale;

	public override void Begin()
	{
		base.duration = timeToArrive + Random.Range(0f, 0.4f);
		EffectData effectData = new EffectData
		{
			scale = scale,
			origin = origin,
			genericFloat = base.duration
		};
		effectData.SetHurtBoxReference(target);
		EffectManager.SpawnEffect(OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/HauntOrbEffect"), effectData, transmit: true);
	}

	public override void OnArrival()
	{
		if ((bool)target)
		{
			HealthComponent healthComponent = target.healthComponent;
			if ((bool)healthComponent && (bool)healthComponent.body)
			{
				healthComponent.body.AddTimedBuff(RoR2Content.Buffs.AffixHaunted, 15f);
			}
		}
	}
}

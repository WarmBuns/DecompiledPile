using UnityEngine;

namespace RoR2.Orbs;

public class GoldOrb : Orb
{
	public uint goldAmount;

	public bool scaleOrb = true;

	public float overrideDuration = 0.6f;

	public override void Begin()
	{
		if ((bool)target)
		{
			base.duration = overrideDuration;
			float scale = (scaleOrb ? Mathf.Min((float)goldAmount / Run.instance.difficultyCoefficient, 1f) : 1f);
			EffectData effectData = new EffectData
			{
				scale = scale,
				origin = origin,
				genericFloat = base.duration
			};
			effectData.SetHurtBoxReference(target);
			EffectManager.SpawnEffect(OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/GoldOrbEffect"), effectData, transmit: true);
		}
	}

	public override void OnArrival()
	{
		if ((bool)target && (bool)target.healthComponent && (bool)target.healthComponent.body && (bool)target.healthComponent.body.master)
		{
			target.healthComponent.body.master.GiveMoney(goldAmount);
		}
	}
}

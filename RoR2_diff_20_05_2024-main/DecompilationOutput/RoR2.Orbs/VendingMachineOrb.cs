using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RoR2.Orbs;

public class VendingMachineOrb : Orb
{
	public float healFraction;

	public bool scaleOrb = true;

	public float speed = 10f;

	public override void Begin()
	{
		if ((bool)target)
		{
			base.duration = Mathf.Max(1f, base.distanceToTarget / speed);
			float scale = (scaleOrb ? Mathf.Min(healFraction, 1f) : 1f);
			EffectData effectData = new EffectData
			{
				scale = scale,
				origin = origin,
				genericFloat = base.duration
			};
			effectData.SetHurtBoxReference(target);
			EffectManager.SpawnEffect(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VendingMachine/VendingMachineOrbEffect.prefab").WaitForCompletion(), effectData, transmit: true);
		}
	}

	public override void OnArrival()
	{
		if ((bool)target)
		{
			HealthComponent healthComponent = target.healthComponent;
			if ((bool)healthComponent)
			{
				healthComponent.HealFraction(healFraction, default(ProcChainMask));
			}
		}
	}
}

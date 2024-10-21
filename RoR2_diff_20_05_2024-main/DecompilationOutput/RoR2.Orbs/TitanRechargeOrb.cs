namespace RoR2.Orbs;

public class TitanRechargeOrb : Orb
{
	public int targetRockInt;

	public TitanRockController titanRockController;

	public override void Begin()
	{
		base.duration = 1f;
		EffectData effectData = new EffectData
		{
			origin = origin,
			genericFloat = base.duration
		};
		effectData.SetHurtBoxReference(target);
		EffectManager.SpawnEffect(OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/HealthOrbEffect"), effectData, transmit: true);
	}

	public override void OnArrival()
	{
	}
}

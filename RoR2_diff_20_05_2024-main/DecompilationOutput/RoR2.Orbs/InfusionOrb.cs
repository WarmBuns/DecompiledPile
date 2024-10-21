namespace RoR2.Orbs;

public class InfusionOrb : Orb
{
	private const float speed = 30f;

	public int maxHpValue;

	private Inventory targetInventory;

	public override void Begin()
	{
		base.duration = base.distanceToTarget / 30f;
		EffectData effectData = new EffectData
		{
			origin = origin,
			genericFloat = base.duration
		};
		effectData.SetHurtBoxReference(target);
		EffectManager.SpawnEffect(OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/InfusionOrbEffect"), effectData, transmit: true);
		CharacterBody characterBody = target.GetComponent<HurtBox>()?.healthComponent.GetComponent<CharacterBody>();
		if ((bool)characterBody)
		{
			targetInventory = characterBody.inventory;
		}
	}

	public override void OnArrival()
	{
		if ((bool)targetInventory)
		{
			targetInventory.AddInfusionBonus((uint)maxHpValue);
		}
	}
}

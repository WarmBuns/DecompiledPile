using UnityEngine;

namespace RoR2.Orbs;

public class MicroMissileOrb : GenericDamageOrb
{
	public override void Begin()
	{
		speed = 55f;
		base.Begin();
	}

	protected override GameObject GetOrbEffect()
	{
		return OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/MicroMissileOrbEffect");
	}
}

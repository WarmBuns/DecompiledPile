using UnityEngine;

namespace RoR2.Orbs;

public class HuntressArrowOrb : GenericDamageOrb
{
	public override void Begin()
	{
		speed = 120f;
		base.Begin();
	}

	protected override GameObject GetOrbEffect()
	{
		return OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/ArrowOrbEffect");
	}
}

using UnityEngine;

namespace RoR2.Orbs;

public class HuntressFlurryArrowOrb : HuntressArrowOrb
{
	public override void Begin()
	{
		base.Begin();
		speed = 80f;
	}

	protected override GameObject GetOrbEffect()
	{
		if (isCrit)
		{
			return OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/FlurryArrowCritOrbEffect");
		}
		return OrbStorageUtility.Get("Prefabs/Effects/OrbEffects/FlurryArrowOrbEffect");
	}
}

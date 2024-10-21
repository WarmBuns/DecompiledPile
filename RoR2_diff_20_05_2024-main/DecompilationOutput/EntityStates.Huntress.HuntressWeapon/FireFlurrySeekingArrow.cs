using RoR2.Orbs;
using UnityEngine;

namespace EntityStates.Huntress.HuntressWeapon;

public class FireFlurrySeekingArrow : FireSeekingArrow
{
	public static GameObject critMuzzleflashEffectPrefab;

	public static int critMaxArrowCount;

	public static float critBaseArrowReloadDuration;

	public override void OnEnter()
	{
		base.OnEnter();
		if (isCrit)
		{
			muzzleflashEffectPrefab = critMuzzleflashEffectPrefab;
			maxArrowCount = critMaxArrowCount;
			arrowReloadDuration = critBaseArrowReloadDuration / attackSpeedStat;
		}
	}

	protected override GenericDamageOrb CreateArrowOrb()
	{
		return new HuntressFlurryArrowOrb();
	}
}

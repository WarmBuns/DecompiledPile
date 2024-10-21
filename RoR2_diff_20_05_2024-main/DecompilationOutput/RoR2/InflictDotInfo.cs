using UnityEngine;

namespace RoR2;

public struct InflictDotInfo
{
	public GameObject victimObject;

	public GameObject attackerObject;

	public DotController.DotIndex dotIndex;

	public float duration;

	public float damageMultiplier;

	public uint? maxStacksFromAttacker;

	public float? totalDamage;

	public DotController.DotIndex? preUpgradeDotIndex;
}

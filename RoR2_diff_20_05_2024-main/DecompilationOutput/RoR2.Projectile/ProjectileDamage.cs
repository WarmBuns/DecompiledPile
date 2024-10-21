using UnityEngine;

namespace RoR2.Projectile;

public class ProjectileDamage : MonoBehaviour
{
	[HideInInspector]
	public float damage;

	[HideInInspector]
	public bool crit;

	[HideInInspector]
	public float force;

	[HideInInspector]
	public DamageColorIndex damageColorIndex;

	public DamageTypeCombo damageType;

	[Tooltip("If true, we cap the maximum stacks for this attacker")]
	public bool useDotMaxStacksFromAttacker;

	[Tooltip("The number of maximum stacks (if we're capping)")]
	public uint dotMaxStacksFromAttacker = uint.MaxValue;

	public void SetDamageTypeViaInt(int newDamageType)
	{
		damageType = new DamageTypeCombo
		{
			damageType = (DamageType)newDamageType
		};
	}
}

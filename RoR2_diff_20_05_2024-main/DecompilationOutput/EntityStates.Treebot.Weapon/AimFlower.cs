using RoR2;
using UnityEngine;

namespace EntityStates.Treebot.Weapon;

public class AimFlower : AimThrowableBase
{
	public static float healthCostFraction;

	private bool keyDown = true;

	public override void Update()
	{
		base.Update();
		keyDown &= !base.inputBank.skill1.down;
	}

	protected override bool KeyIsDown()
	{
		return keyDown;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}

	protected override void FireProjectile()
	{
		if ((bool)base.healthComponent)
		{
			DamageInfo damageInfo = new DamageInfo();
			damageInfo.damage = base.healthComponent.combinedHealth * healthCostFraction;
			damageInfo.position = base.characterBody.corePosition;
			damageInfo.force = Vector3.zero;
			damageInfo.damageColorIndex = DamageColorIndex.Default;
			damageInfo.crit = false;
			damageInfo.attacker = null;
			damageInfo.inflictor = null;
			damageInfo.damageType = DamageType.NonLethal;
			damageInfo.procCoefficient = 0f;
			damageInfo.procChainMask = default(ProcChainMask);
			base.healthComponent.TakeDamage(damageInfo);
		}
		base.FireProjectile();
	}
}

using RoR2;

namespace EntityStates.Bandit2.Weapon;

public class FireSidearmResetRevolver : BaseFireSidearmRevolverState
{
	protected override void ModifyBullet(BulletAttack bulletAttack)
	{
		base.ModifyBullet(bulletAttack);
		bulletAttack.damageType |= (DamageTypeCombo)DamageType.ResetCooldownsOnKill;
	}
}

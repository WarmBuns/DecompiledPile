using EntityStates.Railgunner.Backpack;
using RoR2;

namespace EntityStates.Railgunner.Weapon;

public class FireSnipeCryo : BaseFireSnipe
{
	protected override void ModifyBullet(BulletAttack bulletAttack)
	{
		base.ModifyBullet(bulletAttack);
		bulletAttack.damageType |= (DamageTypeCombo)DamageType.Freeze2s;
	}

	protected override EntityState InstantiateBackpackState()
	{
		return new UseCryo();
	}
}

using RoR2;

namespace EntityStates.Bandit2.Weapon;

public class Bandit2FireRifle : Bandit2FirePrimaryBase
{
	protected override void ModifyBullet(BulletAttack bulletAttack)
	{
		base.ModifyBullet(bulletAttack);
		bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
	}
}

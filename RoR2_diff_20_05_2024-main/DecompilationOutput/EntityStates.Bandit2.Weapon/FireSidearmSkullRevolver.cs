using RoR2;

namespace EntityStates.Bandit2.Weapon;

public class FireSidearmSkullRevolver : BaseFireSidearmRevolverState
{
	protected override void ModifyBullet(BulletAttack bulletAttack)
	{
		base.ModifyBullet(bulletAttack);
		int num = 0;
		if ((bool)base.characterBody)
		{
			num = base.characterBody.GetBuffCount(RoR2Content.Buffs.BanditSkull);
		}
		bulletAttack.damage *= 1f + 0.1f * (float)num;
		bulletAttack.damageType |= (DamageTypeCombo)DamageType.GiveSkullOnKill;
	}
}

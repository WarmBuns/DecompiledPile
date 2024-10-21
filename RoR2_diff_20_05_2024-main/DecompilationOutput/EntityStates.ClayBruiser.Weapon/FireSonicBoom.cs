using EntityStates.Treebot.Weapon;
using RoR2;
using UnityEngine;

namespace EntityStates.ClayBruiser.Weapon;

public class FireSonicBoom : EntityStates.Treebot.Weapon.FireSonicBoom
{
	private static int WeaponIsReadyParamHash = Animator.StringToHash("WeaponIsReady");

	public override void OnEnter()
	{
		base.OnEnter();
		GetModelAnimator().SetBool(WeaponIsReadyParamHash, value: true);
	}

	protected override void AddDebuff(CharacterBody body)
	{
		body.AddTimedBuff(RoR2Content.Buffs.ClayGoo, slowDuration);
	}

	public override void OnExit()
	{
		if (!outer.destroying)
		{
			GetModelAnimator().SetBool(WeaponIsReadyParamHash, value: false);
		}
		base.OnExit();
	}
}

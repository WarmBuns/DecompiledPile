using RoR2;
using UnityEngine;

namespace EntityStates.ClayBruiser.Weapon;

public class MinigunSpinDown : MinigunState
{
	public static float baseDuration;

	public static string sound;

	private float duration;

	private static int WeaponIsReadyParamHash = Animator.StringToHash("WeaponIsReady");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Util.PlayAttackSpeedSound(sound, base.gameObject, attackSpeedStat);
		GetModelAnimator().SetBool(WeaponIsReadyParamHash, value: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}

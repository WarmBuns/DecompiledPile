using RoR2;

namespace EntityStates.UrchinTurret.Weapon;

public class MinigunSpinDown : MinigunState
{
	public static float baseDuration;

	public static string sound;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Util.PlayAttackSpeedSound(sound, base.gameObject, attackSpeedStat);
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

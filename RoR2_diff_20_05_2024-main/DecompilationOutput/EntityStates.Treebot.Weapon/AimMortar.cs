using RoR2;
using UnityEngine;

namespace EntityStates.Treebot.Weapon;

public class AimMortar : AimThrowableBase
{
	public static string enterSoundString;

	public static string exitSoundString;

	private static int StateHash = Animator.StringToHash("");

	private static int ParamHash = Animator.StringToHash(".playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(enterSoundString, base.gameObject);
		PlayAnimation("Gesture, Additive", StateHash, ParamHash, minimumDuration);
	}

	public override void OnExit()
	{
		base.OnExit();
		outer.SetNextState(new FireMortar());
		if (!outer.destroying)
		{
			Util.PlaySound(exitSoundString, base.gameObject);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

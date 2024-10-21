using RoR2;
using UnityEngine;

namespace EntityStates.BeetleGuardMonster;

public class SpawnState : BaseState
{
	public static float duration = 4f;

	public static string spawnSoundString;

	private static int Spawn1StateHash = Animator.StringToHash("Spawn1");

	private static int Spawn1ParamHash = Animator.StringToHash("Spawn1.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(spawnSoundString, base.gameObject);
		PlayAnimation("Body", Spawn1StateHash, Spawn1ParamHash, duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Death;
	}
}

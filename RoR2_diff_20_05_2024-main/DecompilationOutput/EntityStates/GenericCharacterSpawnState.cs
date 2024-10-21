using RoR2;
using UnityEngine;

namespace EntityStates;

public abstract class GenericCharacterSpawnState : BaseState
{
	[SerializeField]
	public float duration = 2f;

	[SerializeField]
	public string spawnSoundString;

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

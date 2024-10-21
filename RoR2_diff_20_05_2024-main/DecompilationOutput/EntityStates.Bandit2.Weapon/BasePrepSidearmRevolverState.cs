using RoR2;
using UnityEngine;

namespace EntityStates.Bandit2.Weapon;

public abstract class BasePrepSidearmRevolverState : BaseSidearmState
{
	[SerializeField]
	public string enterSoundString;

	private Animator animator;

	private static int MainToSideStateHash = Animator.StringToHash("MainToSide");

	private static int MainToSideParamHash = Animator.StringToHash("MainToSide.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Gesture, Additive", MainToSideStateHash, MainToSideParamHash, duration);
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextState(GetNextState());
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	protected abstract EntityState GetNextState();
}

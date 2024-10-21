using RoR2;
using UnityEngine;

namespace EntityStates.Barrel;

public class Opening : EntityState
{
	public static float duration = 1f;

	private static int OpeningStateHash = Animator.StringToHash("Opening");

	private static int OpeningParamHash = Animator.StringToHash("Opening.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Body", OpeningStateHash, OpeningParamHash, duration);
		if ((bool)base.sfxLocator)
		{
			Util.PlaySound(base.sfxLocator.openSound, base.gameObject);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(new Opened());
		}
	}
}

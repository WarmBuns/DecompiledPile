using RoR2;
using UnityEngine;

namespace EntityStates;

public class HurtState : BaseState
{
	private float stopwatch;

	private float duration = 0.35f;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)base.sfxLocator && base.sfxLocator.barkSound != "")
		{
			Util.PlaySound(base.sfxLocator.barkSound, base.gameObject);
		}
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			int layerIndex = modelAnimator.GetLayerIndex("Body");
			modelAnimator.CrossFadeInFixedTime((Random.Range(0, 2) == 0) ? "Hurt1" : "Hurt2", 0.1f);
			modelAnimator.Update(0f);
			duration = modelAnimator.GetNextAnimatorStateInfo(layerIndex).length;
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.isSprinting = false;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}

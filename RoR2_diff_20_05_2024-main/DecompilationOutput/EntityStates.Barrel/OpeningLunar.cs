using RoR2;
using UnityEngine;

namespace EntityStates.Barrel;

public class OpeningLunar : BaseState
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
		StopSteamEffect();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(new Opened());
		}
	}

	private void StopSteamEffect()
	{
		Transform modelTransform = GetModelTransform();
		if (!modelTransform)
		{
			return;
		}
		ChildLocator component = modelTransform.GetComponent<ChildLocator>();
		if (!component)
		{
			return;
		}
		Transform transform = component.FindChild("SteamEffect");
		if ((bool)transform)
		{
			ParticleSystem component2 = transform.GetComponent<ParticleSystem>();
			if ((bool)component2)
			{
				ParticleSystem.MainModule main = component2.main;
				main.loop = false;
			}
		}
	}
}

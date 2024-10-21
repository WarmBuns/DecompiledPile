using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.BrotherMonster;

public class SpellChannelExitState : SpellBaseState
{
	public static float lendInterval;

	public static float duration;

	public static GameObject channelFinishEffectPrefab;

	private static int SpellChannelExitStateHash = Animator.StringToHash("SpellChannelExit");

	private static int SpellChannelExitParamHash = Animator.StringToHash("SpellChannelExit.playbackRate");

	protected override bool DisplayWeapon => false;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Body", SpellChannelExitStateHash, SpellChannelExitParamHash, duration);
		if (NetworkServer.active && (bool)itemStealController)
		{
			itemStealController.stealInterval = lendInterval;
			itemStealController.LendImmediately(base.characterBody.inventory);
		}
		if ((bool)channelFinishEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(channelFinishEffectPrefab, base.gameObject, "SpellChannel", transmit: false);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		SetStateOnHurt component = GetComponent<SetStateOnHurt>();
		if ((bool)component)
		{
			component.canBeFrozen = true;
		}
		base.OnExit();
	}
}

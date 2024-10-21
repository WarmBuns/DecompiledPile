using RoR2;
using UnityEngine;

namespace EntityStates.BrotherMonster;

public class UltExitState : BaseState
{
	public static float lendInterval;

	public static float duration;

	public static string soundString;

	public static GameObject channelFinishEffectPrefab;

	private static int UltExitStateHash = Animator.StringToHash("UltExit");

	private static int UltParamHash = Animator.StringToHash("Ult.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Body", UltExitStateHash, UltParamHash, duration);
		Util.PlaySound(soundString, base.gameObject);
		if ((bool)channelFinishEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(channelFinishEffectPrefab, base.gameObject, "MuzzleUlt", transmit: false);
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
		GenericSkill genericSkill = (base.skillLocator ? base.skillLocator.special : null);
		if ((bool)genericSkill)
		{
			genericSkill.UnsetSkillOverride(outer, UltChannelState.replacementSkillDef, GenericSkill.SkillOverridePriority.Contextual);
		}
		base.OnExit();
	}
}

using RoR2;
using UnityEngine;

namespace EntityStates.BrotherMonster;

public class SpellChannelEnterState : SpellBaseState
{
	public static GameObject channelBeginEffectPrefab;

	public static float duration;

	private Transform trueDeathEffect;

	private static int SpellChannerEnterStateHash = Animator.StringToHash("SpellChannelEnter");

	private static int SpellChannelEnterParamHash = Animator.StringToHash("SpellChannelEnter.playbackRate");

	protected override bool DisplayWeapon => false;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Body", SpellChannerEnterStateHash, SpellChannelEnterParamHash, duration);
		Util.PlaySound("Play_moonBrother_phase4_transition", base.gameObject);
		trueDeathEffect = FindModelChild("TrueDeathEffect");
		if ((bool)trueDeathEffect)
		{
			trueDeathEffect.gameObject.SetActive(value: true);
			trueDeathEffect.GetComponent<ScaleParticleSystemDuration>().newDuration = 10f;
		}
		HurtBoxGroup component = GetModelTransform().GetComponent<HurtBoxGroup>();
		if ((bool)component)
		{
			int hurtBoxesDeactivatorCounter = component.hurtBoxesDeactivatorCounter + 1;
			component.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge > duration)
		{
			outer.SetNextState(new SpellChannelState());
		}
	}

	public override void OnExit()
	{
		HurtBoxGroup component = GetModelTransform().GetComponent<HurtBoxGroup>();
		if ((bool)component)
		{
			int hurtBoxesDeactivatorCounter = component.hurtBoxesDeactivatorCounter - 1;
			component.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
		}
		if ((bool)channelBeginEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(channelBeginEffectPrefab, base.gameObject, "SpellChannel", transmit: false);
		}
		if ((bool)trueDeathEffect)
		{
			trueDeathEffect.gameObject.SetActive(value: false);
		}
		base.OnExit();
	}
}

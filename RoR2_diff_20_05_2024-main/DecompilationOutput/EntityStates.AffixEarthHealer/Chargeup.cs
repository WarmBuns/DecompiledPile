using RoR2;
using UnityEngine;

namespace EntityStates.AffixEarthHealer;

public class Chargeup : BaseState
{
	private static int chargeUpAnimationStateHash = Animator.StringToHash("ChargeUp");

	private static int chargeUpParamHash = Animator.StringToHash("ChargeUp.playbackRate");

	public static float duration;

	public static string enterSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		FindModelChild("ChargeUpFX").gameObject.SetActive(value: true);
		PlayAnimation("Base", chargeUpAnimationStateHash, chargeUpParamHash, duration);
		Util.PlaySound(enterSoundString, base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextState(new Heal());
		}
	}

	public override void OnExit()
	{
		FindModelChild("ChargeUpFX").gameObject.SetActive(value: false);
		base.OnExit();
	}
}

using EntityStates.Mage.Weapon;
using UnityEngine;

namespace EntityStates.GlobalSkills.LunarNeedle;

public class ChargeLunarSecondary : BaseChargeBombState
{
	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string playbackRateParam;

	protected override BaseThrowBombState GetNextState()
	{
		return new ThrowLunarSecondary();
	}

	protected override void PlayChargeAnimation()
	{
		PlayAnimation(animationLayerName, animationStateName, playbackRateParam, base.duration);
	}
}

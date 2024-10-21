using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.InfiniteTowerSafeWard;

public class AwaitingActivation : BaseSafeWardState
{
	[SerializeField]
	public float radius;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string enterSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName);
		Util.PlaySound(enterSoundString, base.gameObject);
		if ((bool)purchaseInteraction)
		{
			purchaseInteraction.SetAvailable(newAvailable: true);
		}
		if ((bool)zone)
		{
			zone.Networkradius = radius;
		}
	}

	public void Activate()
	{
		if (NetworkServer.active)
		{
			outer.SetNextState(new Active());
		}
	}
}

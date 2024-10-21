using RoR2;
using UnityEngine;

namespace EntityStates.InfiniteTowerSafeWard;

public class AwaitingPortalUse : BaseSafeWardState
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
			purchaseInteraction.SetAvailable(newAvailable: false);
		}
		if ((bool)zone)
		{
			zone.Networkradius = radius;
		}
	}
}

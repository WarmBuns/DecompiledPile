using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.InfiniteTowerSafeWard;

public class CorrallingPlayers : BaseSafeWardState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public float initialRadius;

	[SerializeField]
	public float finalRadius;

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
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)zone)
		{
			float t = Mathf.Min(1f, base.fixedAge / duration);
			zone.Networkradius = Mathf.Lerp(initialRadius, finalRadius, t);
		}
		if (NetworkServer.active && base.fixedAge >= duration)
		{
			outer.SetNextState(new AwaitingActivation());
		}
	}
}

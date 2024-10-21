using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.InfiniteTowerSafeWard;

public class Active : BaseSafeWardState
{
	[SerializeField]
	public float radius;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string enterSoundString;

	private InfiniteTowerRun run;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)safeWardController)
		{
			safeWardController.SetIndicatorEnabled(enabled: false);
		}
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
		run = Run.instance as InfiniteTowerRun;
	}

	public void SelfDestruct()
	{
		if (NetworkServer.active)
		{
			outer.SetNextState(new SelfDestruct());
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)zone && (bool)run)
		{
			float num = 1f;
			if ((bool)run.waveController)
			{
				num = run.waveController.zoneRadiusPercentage;
			}
			zone.Networkradius = radius * num;
		}
	}
}

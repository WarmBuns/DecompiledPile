using System;
using RoR2;
using UnityEngine;

namespace EntityStates.TimedChest;

public class Opening : BaseState
{
	public static float delayUntilUnlockAchievement;

	private bool hasGrantedAchievement;

	private static int OpeningStateHash = Animator.StringToHash("Opening");

	public static event Action onOpened;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Body", OpeningStateHash);
		TimedChestController component = GetComponent<TimedChestController>();
		if ((bool)component)
		{
			component.purchased = true;
		}
		if ((bool)base.sfxLocator)
		{
			Util.PlaySound(base.sfxLocator.openSound, base.gameObject);
		}
		SetPingable(value: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= delayUntilUnlockAchievement && !hasGrantedAchievement)
		{
			Opening.onOpened?.Invoke();
			hasGrantedAchievement = true;
			GenericPickupController componentInChildren = base.gameObject.GetComponentInChildren<GenericPickupController>();
			if ((bool)componentInChildren)
			{
				componentInChildren.enabled = true;
			}
		}
	}
}

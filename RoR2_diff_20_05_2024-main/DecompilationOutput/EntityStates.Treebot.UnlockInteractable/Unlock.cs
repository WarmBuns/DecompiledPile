using System;
using RoR2;
using RoR2.Mecanim;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Treebot.UnlockInteractable;

public class Unlock : BaseState
{
	private static int ReviveParamHash = Animator.StringToHash("Revive");

	public static event Action<Interactor> onActivated;

	public override void OnEnter()
	{
		base.OnEnter();
		if (NetworkServer.active)
		{
			Unlock.onActivated?.Invoke(GetComponent<PurchaseInteraction>().lastActivator);
		}
		GetModelAnimator().SetBool(ReviveParamHash, value: true);
		GetModelTransform().GetComponent<RandomBlinkController>().enabled = true;
	}
}

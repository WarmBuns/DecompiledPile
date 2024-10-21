using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.SurvivorPod;

public class Release : SurvivorPodBaseState
{
	public static float ejectionSpeed = 20f;

	private static int ReleaseStateHash = Animator.StringToHash("Release");

	public static event Action<Release, CharacterBody> onEnter;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Base", ReleaseStateHash);
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			component.FindChild("Door").gameObject.SetActive(value: false);
			component.FindChild("ReleaseExhaustFX").gameObject.SetActive(value: true);
		}
		Release.onEnter?.Invoke(this, base.vehicleSeat?.currentPassengerBody);
		if ((bool)base.survivorPodController && NetworkServer.active && (bool)base.vehicleSeat && (bool)base.vehicleSeat.currentPassengerBody)
		{
			base.vehicleSeat.EjectPassenger(base.vehicleSeat.currentPassengerBody.gameObject);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && (!base.vehicleSeat || !base.vehicleSeat.currentPassengerBody))
		{
			outer.SetNextState(new ReleaseFinished());
		}
	}
}

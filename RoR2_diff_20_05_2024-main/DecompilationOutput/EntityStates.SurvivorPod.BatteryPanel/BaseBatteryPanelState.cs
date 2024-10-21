using RoR2;
using UnityEngine;

namespace EntityStates.SurvivorPod.BatteryPanel;

public class BaseBatteryPanelState : BaseState
{
	protected struct PodInfo
	{
		public GameObject podObject;

		public Animator podAnimator;
	}

	protected PodInfo podInfo;

	public override void OnEnter()
	{
		base.OnEnter();
		SetPodObject(base.gameObject.GetComponentInParent<VehicleSeat>()?.gameObject);
	}

	private void SetPodObject(GameObject podObject)
	{
		podInfo = default(PodInfo);
		if (!podObject)
		{
			return;
		}
		podInfo.podObject = podObject;
		ModelLocator component = podObject.GetComponent<ModelLocator>();
		if ((bool)component)
		{
			Transform modelTransform = component.modelTransform;
			if ((bool)modelTransform)
			{
				podInfo.podAnimator = modelTransform.GetComponent<Animator>();
			}
		}
	}

	protected void PlayPodAnimation(string layerName, string animationStateName, string playbackRateParam, float duration)
	{
		if ((bool)podInfo.podAnimator)
		{
			EntityState.PlayAnimationOnAnimator(podInfo.podAnimator, layerName, animationStateName, playbackRateParam, duration);
		}
	}

	protected void PlayPodAnimation(string layerName, string animationStateName)
	{
		if ((bool)podInfo.podAnimator)
		{
			EntityState.PlayAnimationOnAnimator(podInfo.podAnimator, layerName, animationStateName);
		}
	}

	protected void EnablePickup()
	{
		ChildLocator component = podInfo.podAnimator.GetComponent<ChildLocator>();
		if (!component)
		{
			return;
		}
		Transform transform = component.FindChild("BatteryAttachmentPoint");
		if (!transform)
		{
			return;
		}
		Transform transform2 = transform.Find("QuestVolatileBatteryWorldPickup(Clone)");
		if ((bool)transform2)
		{
			GenericPickupController component2 = transform2.GetComponent<GenericPickupController>();
			if ((bool)component2)
			{
				component2.enabled = true;
			}
		}
	}
}

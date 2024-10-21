using EntityStates.SurvivorPod;
using RoR2;
using UnityEngine;

namespace EntityStates.VoidSurvivorPod;

public class Landed : SurvivorPodBaseState
{
	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public string effectMuzzle;

	[SerializeField]
	public GameObject openEffect;

	[SerializeField]
	public string particleChildName;

	private ParticleSystem[] particles;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName);
		Util.PlaySound(enterSoundString, base.gameObject);
		base.vehicleSeat.handleVehicleExitRequestServer.AddCallback(HandleVehicleExitRequest);
		ModelLocator component = GetComponent<ModelLocator>();
		if (!component || !component.modelTransform)
		{
			return;
		}
		ChildLocator component2 = component.modelTransform.GetComponent<ChildLocator>();
		if ((bool)component2)
		{
			Transform transform = component2.FindChild(particleChildName);
			if ((bool)transform)
			{
				particles = transform.GetComponentsInChildren<ParticleSystem>();
				transform.gameObject.SetActive(value: true);
			}
		}
	}

	public override void FixedUpdate()
	{
		if (base.fixedAge > 0f)
		{
			base.survivorPodController.exitAllowed = true;
		}
		base.FixedUpdate();
	}

	private void HandleVehicleExitRequest(GameObject gameObject, ref bool? result)
	{
		base.survivorPodController.exitAllowed = false;
		outer.SetNextState(new Release());
		result = true;
	}

	public override void OnExit()
	{
		base.vehicleSeat.handleVehicleExitRequestServer.RemoveCallback(HandleVehicleExitRequest);
		base.survivorPodController.exitAllowed = false;
		EffectManager.SimpleMuzzleFlash(openEffect, base.gameObject, effectMuzzle, transmit: false);
		ParticleSystem[] array = particles;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Stop();
		}
	}
}

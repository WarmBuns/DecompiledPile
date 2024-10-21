using EntityStates.SurvivorPod;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidSurvivorPod;

public class Release : SurvivorPodBaseState
{
	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public float exitForceAmount;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName);
		Util.PlaySound(enterSoundString, base.gameObject);
		if ((bool)base.survivorPodController && NetworkServer.active && (bool)base.vehicleSeat && (bool)base.vehicleSeat.currentPassengerBody)
		{
			CharacterBody currentPassengerBody = base.vehicleSeat.currentPassengerBody;
			base.vehicleSeat.EjectPassenger(currentPassengerBody.gameObject);
			HealthComponent component = currentPassengerBody.GetComponent<HealthComponent>();
			if ((bool)component)
			{
				DamageInfo damageInfo = new DamageInfo();
				damageInfo.attacker = base.gameObject;
				damageInfo.force = Vector3.up * exitForceAmount;
				damageInfo.position = currentPassengerBody.corePosition;
				component.TakeDamageForce(damageInfo, alwaysApply: true);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && (!base.vehicleSeat || !base.vehicleSeat.currentPassengerBody))
		{
			outer.SetNextStateToMain();
		}
	}
}

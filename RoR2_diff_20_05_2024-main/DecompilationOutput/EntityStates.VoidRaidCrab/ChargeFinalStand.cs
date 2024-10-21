using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidRaidCrab;

public class ChargeFinalStand : BaseWardWipeState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public GameObject chargeEffectPrefab;

	[SerializeField]
	public string muzzleName;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public string enterSoundString;

	private GameObject chargeEffectInstance;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		Util.PlaySound(enterSoundString, base.gameObject);
		ChildLocator modelChildLocator = GetModelChildLocator();
		if ((bool)modelChildLocator && (bool)chargeEffectPrefab)
		{
			Transform transform = modelChildLocator.FindChild(muzzleName) ?? base.characterBody.coreTransform;
			if ((bool)transform)
			{
				chargeEffectInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
				chargeEffectInstance.transform.parent = transform;
				ScaleParticleSystemDuration component = chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
				if ((bool)component)
				{
					component.newDuration = duration;
				}
			}
		}
		fogDamageController = GetComponent<FogDamageController>();
		fogDamageController.enabled = true;
		safeWards = new List<GameObject>();
		PhasedInventorySetter component2 = GetComponent<PhasedInventorySetter>();
		if ((bool)component2 && NetworkServer.active)
		{
			component2.AdvancePhase();
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			FireFinalStand nextState = new FireFinalStand();
			outer.SetNextState(nextState);
		}
	}

	public override void OnExit()
	{
		EntityState.Destroy(chargeEffectInstance);
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Pain;
	}
}

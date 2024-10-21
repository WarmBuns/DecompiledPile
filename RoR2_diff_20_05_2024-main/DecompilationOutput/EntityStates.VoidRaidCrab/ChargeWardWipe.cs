using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidRaidCrab;

public class ChargeWardWipe : BaseWardWipeState
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

	[SerializeField]
	public InteractableSpawnCard safeWardSpawnCard;

	[SerializeField]
	public AnimationCurve safeWardSpawnCurve;

	[SerializeField]
	public float minDistanceBetweenConsecutiveWards;

	[SerializeField]
	public float maxDistanceBetweenConsecutiveWards;

	[SerializeField]
	public float maxDistanceToInitialWard;

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
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active && (bool)safeWardSpawnCard)
		{
			float time = base.fixedAge / duration;
			float f = safeWardSpawnCurve.Evaluate(time);
			DirectorPlacementRule directorPlacementRule = null;
			while ((float)safeWards.Count < Mathf.Floor(f))
			{
				if (directorPlacementRule == null)
				{
					directorPlacementRule = new DirectorPlacementRule
					{
						placementMode = DirectorPlacementRule.PlacementMode.Approximate,
						maxDistance = maxDistanceToInitialWard,
						minDistance = 0f,
						spawnOnTarget = base.gameObject.transform,
						preventOverhead = true
					};
				}
				if (safeWards.Count > 0)
				{
					directorPlacementRule.maxDistance = maxDistanceBetweenConsecutiveWards;
					directorPlacementRule.minDistance = minDistanceBetweenConsecutiveWards;
					directorPlacementRule.spawnOnTarget = safeWards[safeWards.Count - 1].transform;
					directorPlacementRule.placementMode = DirectorPlacementRule.PlacementMode.Approximate;
				}
				GameObject gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(safeWardSpawnCard, directorPlacementRule, Run.instance.stageRng));
				if ((bool)gameObject)
				{
					NetworkServer.Spawn(gameObject);
					if ((bool)fogDamageController)
					{
						IZone component = gameObject.GetComponent<IZone>();
						fogDamageController.AddSafeZone(component);
					}
					safeWards.Add(gameObject);
				}
				else
				{
					Debug.LogError("Unable to spawn safe ward instance.  Are there any ground nodes?");
				}
			}
		}
		if (base.isAuthority && base.fixedAge >= duration)
		{
			FireWardWipe nextState = new FireWardWipe();
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

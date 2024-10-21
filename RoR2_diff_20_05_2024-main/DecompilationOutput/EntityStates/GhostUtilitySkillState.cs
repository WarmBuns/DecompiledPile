using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates;

public class GhostUtilitySkillState : GenericCharacterMain
{
	public static float baseDuration;

	public static GameObject coreVfxPrefab;

	public static GameObject footVfxPrefab;

	public static GameObject entryEffectPrefab;

	public static GameObject exitEffectPrefab;

	public static float moveSpeedCoefficient;

	public static float healFractionPerTick;

	public static float healFrequency;

	private HurtBoxGroup hurtBoxGroup;

	private CharacterModel characterModel;

	private GameObject coreVfxInstance;

	private GameObject footVfxInstance;

	private float healTimer;

	private float duration;

	private ICharacterGravityParameterProvider characterGravityParameterProvider;

	private ICharacterFlightParameterProvider characterFlightParameterProvider;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string playbackRateParam;

	private EffectManagerHelper _emh_coreVfxInstance;

	private EffectManagerHelper _emh_footVfxInstance;

	public override void Reset()
	{
		base.Reset();
		hurtBoxGroup = null;
		characterModel = null;
		coreVfxInstance = null;
		footVfxInstance = null;
		healTimer = 0f;
		duration = 0f;
		_emh_coreVfxInstance = null;
		_emh_footVfxInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
		characterGravityParameterProvider = base.gameObject.GetComponent<ICharacterGravityParameterProvider>();
		characterFlightParameterProvider = base.gameObject.GetComponent<ICharacterFlightParameterProvider>();
		if ((bool)base.characterBody)
		{
			if ((bool)base.characterBody.inventory)
			{
				duration *= base.characterBody.inventory.GetItemCount(RoR2Content.Items.LunarUtilityReplacement);
			}
			hurtBoxGroup = base.characterBody.hurtBoxGroup;
			if ((bool)hurtBoxGroup)
			{
				HurtBoxGroup obj = hurtBoxGroup;
				int hurtBoxesDeactivatorCounter = obj.hurtBoxesDeactivatorCounter + 1;
				obj.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
			}
			if ((bool)coreVfxPrefab)
			{
				if (!EffectManager.ShouldUsePooledEffect(coreVfxPrefab))
				{
					coreVfxInstance = Object.Instantiate(coreVfxPrefab);
				}
				else
				{
					_emh_coreVfxInstance = EffectManager.GetAndActivatePooledEffect(coreVfxPrefab, Vector3.zero, Quaternion.identity);
					coreVfxInstance = _emh_coreVfxInstance.gameObject;
				}
			}
			if ((bool)footVfxPrefab)
			{
				if (!EffectManager.ShouldUsePooledEffect(footVfxPrefab))
				{
					footVfxInstance = Object.Instantiate(footVfxPrefab);
				}
				else
				{
					_emh_footVfxInstance = EffectManager.GetAndActivatePooledEffect(footVfxPrefab, Vector3.zero, Quaternion.identity);
					footVfxInstance = _emh_footVfxInstance.gameObject;
				}
			}
			UpdateVfxPositions();
			if ((bool)entryEffectPrefab)
			{
				Ray aimRay = GetAimRay();
				EffectManager.SimpleEffect(entryEffectPrefab, aimRay.origin, Quaternion.LookRotation(aimRay.direction), transmit: false);
			}
		}
		characterModel = GetModelTransform()?.GetComponent<CharacterModel>();
		if ((bool)base.modelAnimator)
		{
			base.modelAnimator.enabled = false;
		}
		if ((bool)base.characterMotor)
		{
			base.characterMotor.walkSpeedPenaltyCoefficient = moveSpeedCoefficient;
		}
		if (characterGravityParameterProvider != null)
		{
			CharacterGravityParameters gravityParameters = characterGravityParameterProvider.gravityParameters;
			gravityParameters.channeledAntiGravityGranterCount++;
			characterGravityParameterProvider.gravityParameters = gravityParameters;
		}
		if (characterFlightParameterProvider != null)
		{
			CharacterFlightParameters flightParameters = characterFlightParameterProvider.flightParameters;
			flightParameters.channeledFlightGranterCount++;
			characterFlightParameterProvider.flightParameters = flightParameters;
		}
		if ((bool)characterModel)
		{
			characterModel.invisibilityCount++;
		}
		EntityStateMachine[] components = base.gameObject.GetComponents<EntityStateMachine>();
		foreach (EntityStateMachine entityStateMachine in components)
		{
			if (entityStateMachine.customName == "Weapon")
			{
				entityStateMachine.SetNextStateToMain();
			}
		}
	}

	private void UpdateVfxPositions()
	{
		if ((bool)base.characterBody)
		{
			if ((bool)coreVfxInstance)
			{
				coreVfxInstance.transform.position = base.characterBody.corePosition;
			}
			if ((bool)footVfxInstance)
			{
				footVfxInstance.transform.position = base.characterBody.footPosition;
			}
		}
	}

	protected override bool CanExecuteSkill(GenericSkill skillSlot)
	{
		return false;
	}

	public override void Update()
	{
		base.Update();
		UpdateVfxPositions();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		healTimer -= GetDeltaTime();
		if (healTimer <= 0f)
		{
			if (NetworkServer.active)
			{
				base.healthComponent.HealFraction(healFractionPerTick, default(ProcChainMask));
			}
			healTimer = 1f / healFrequency;
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		if ((bool)exitEffectPrefab && !outer.destroying)
		{
			Ray aimRay = GetAimRay();
			EffectManager.SimpleEffect(exitEffectPrefab, aimRay.origin, Quaternion.LookRotation(aimRay.direction), transmit: false);
		}
		if (_emh_coreVfxInstance != null && _emh_coreVfxInstance.OwningPool != null)
		{
			_emh_coreVfxInstance.OwningPool.ReturnObject(_emh_coreVfxInstance);
		}
		else if (coreVfxInstance != null)
		{
			EntityState.Destroy(coreVfxInstance);
		}
		coreVfxInstance = null;
		_emh_coreVfxInstance = null;
		if (_emh_footVfxInstance != null && _emh_footVfxInstance.OwningPool != null)
		{
			_emh_footVfxInstance.OwningPool.ReturnObject(_emh_footVfxInstance);
		}
		else if (footVfxInstance != null)
		{
			EntityState.Destroy(footVfxInstance);
		}
		footVfxInstance = null;
		_emh_footVfxInstance = null;
		if ((bool)characterModel)
		{
			characterModel.invisibilityCount--;
		}
		if ((bool)hurtBoxGroup)
		{
			HurtBoxGroup obj = hurtBoxGroup;
			int hurtBoxesDeactivatorCounter = obj.hurtBoxesDeactivatorCounter - 1;
			obj.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
		}
		if ((bool)base.modelAnimator)
		{
			base.modelAnimator.enabled = true;
		}
		if (characterFlightParameterProvider != null)
		{
			CharacterFlightParameters flightParameters = characterFlightParameterProvider.flightParameters;
			flightParameters.channeledFlightGranterCount--;
			characterFlightParameterProvider.flightParameters = flightParameters;
		}
		if (characterGravityParameterProvider != null)
		{
			CharacterGravityParameters gravityParameters = characterGravityParameterProvider.gravityParameters;
			gravityParameters.channeledAntiGravityGranterCount--;
			characterGravityParameterProvider.gravityParameters = gravityParameters;
		}
		if ((bool)base.characterMotor)
		{
			base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
		}
		base.OnExit();
	}
}

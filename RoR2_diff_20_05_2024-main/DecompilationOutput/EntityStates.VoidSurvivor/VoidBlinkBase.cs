using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidSurvivor;

public class VoidBlinkBase : BaseState
{
	public class VoidBlinkUp : VoidBlinkBase
	{
	}

	public class VoidBlinkDown : VoidBlinkBase
	{
	}

	private Transform modelTransform;

	[SerializeField]
	public GameObject blinkEffectPrefab;

	[SerializeField]
	public float duration = 0.3f;

	[SerializeField]
	public float speedCoefficient = 25f;

	[SerializeField]
	public string beginSoundString;

	[SerializeField]
	public string endSoundString;

	[SerializeField]
	public AnimationCurve forwardSpeed;

	[SerializeField]
	public AnimationCurve upSpeed;

	[SerializeField]
	public GameObject blinkVfxPrefab;

	[SerializeField]
	public float overlayDuration;

	[SerializeField]
	public Material overlayMaterial;

	private CharacterModel characterModel;

	private HurtBoxGroup hurtboxGroup;

	private Vector3 forwardVector;

	private GameObject blinkVfxInstance;

	private uint soundID;

	public override void OnEnter()
	{
		base.OnEnter();
		base.characterBody.bodyFlags |= CharacterBody.BodyFlags.OverheatImmune;
		soundID = Util.PlaySound(beginSoundString, base.gameObject);
		forwardVector = ((base.inputBank.moveVector == Vector3.zero) ? base.characterDirection.forward : base.inputBank.moveVector).normalized;
		modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			characterModel = modelTransform.GetComponent<CharacterModel>();
			hurtboxGroup = modelTransform.GetComponent<HurtBoxGroup>();
		}
		if ((bool)characterModel)
		{
			characterModel.invisibilityCount++;
		}
		if ((bool)hurtboxGroup)
		{
			HurtBoxGroup hurtBoxGroup = hurtboxGroup;
			int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter + 1;
			hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
		}
		if (NetworkServer.active)
		{
			Util.CleanseBody(base.characterBody, removeDebuffs: true, removeBuffs: false, removeCooldownBuffs: false, removeDots: true, removeStun: true, removeNearbyProjectiles: false);
		}
		blinkVfxInstance = Object.Instantiate(blinkVfxPrefab);
		blinkVfxInstance.transform.SetParent(base.transform, worldPositionStays: false);
		CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
	}

	protected virtual Vector3 GetBlinkVector()
	{
		return base.inputBank.aimDirection;
	}

	private void CreateBlinkEffect(Vector3 origin)
	{
		EffectData effectData = new EffectData();
		effectData.rotation = Util.QuaternionSafeLookRotation(GetVelocity());
		effectData.origin = origin;
		EffectManager.SpawnEffect(blinkEffectPrefab, effectData, transmit: false);
	}

	private Vector3 GetVelocity()
	{
		float time = base.fixedAge / duration;
		Vector3 vector = forwardSpeed.Evaluate(time) * forwardVector;
		Vector3 vector2 = upSpeed.Evaluate(time) * Vector3.up;
		return (vector + vector2) * speedCoefficient * moveSpeedStat;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)base.characterMotor && (bool)base.characterDirection)
		{
			if ((bool)base.characterMotor)
			{
				base.characterMotor.Motor.ForceUnground();
			}
			Vector3 velocity = GetVelocity();
			base.characterMotor.velocity = velocity;
			if ((bool)blinkVfxInstance)
			{
				blinkVfxInstance.transform.forward = velocity;
			}
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		AkSoundEngine.StopPlayingID(soundID);
		if (!outer.destroying)
		{
			Util.PlaySound(endSoundString, base.gameObject);
			CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
		}
		if ((bool)blinkVfxInstance)
		{
			VfxKillBehavior.KillVfxObject(blinkVfxInstance);
		}
		if ((bool)characterModel)
		{
			characterModel.invisibilityCount--;
		}
		if ((bool)hurtboxGroup)
		{
			HurtBoxGroup hurtBoxGroup = hurtboxGroup;
			int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter - 1;
			hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
		}
		base.characterBody.bodyFlags &= ~CharacterBody.BodyFlags.OverheatImmune;
		modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
			temporaryOverlayInstance.duration = overlayDuration;
			temporaryOverlayInstance.animateShaderAlpha = true;
			temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
			temporaryOverlayInstance.destroyComponentOnEnd = true;
			temporaryOverlayInstance.originalMaterial = overlayMaterial;
			temporaryOverlayInstance.AddToCharacterModel(modelTransform.GetComponent<CharacterModel>());
		}
		base.OnExit();
	}
}

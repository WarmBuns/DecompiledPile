using RoR2;
using UnityEngine;

namespace EntityStates.Huntress;

public class BlinkState : BaseState
{
	private Transform modelTransform;

	public static GameObject blinkPrefab;

	private float stopwatch;

	private Vector3 blinkVector = Vector3.zero;

	[SerializeField]
	public float duration = 0.3f;

	[SerializeField]
	public float speedCoefficient = 25f;

	[SerializeField]
	public string beginSoundString;

	[SerializeField]
	public string endSoundString;

	private CharacterModel characterModel;

	private HurtBoxGroup hurtboxGroup;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(beginSoundString, base.gameObject);
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
		blinkVector = GetBlinkVector();
		CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
	}

	protected virtual Vector3 GetBlinkVector()
	{
		return base.inputBank.aimDirection;
	}

	private void CreateBlinkEffect(Vector3 origin)
	{
		EffectData effectData = new EffectData();
		effectData.rotation = Util.QuaternionSafeLookRotation(blinkVector);
		effectData.origin = origin;
		EffectManager.SpawnEffect(blinkPrefab, effectData, transmit: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if ((bool)base.characterMotor && (bool)base.characterDirection)
		{
			base.characterMotor.velocity = Vector3.zero;
			base.characterMotor.rootMotion += blinkVector * (moveSpeedStat * speedCoefficient * GetDeltaTime());
		}
		if (stopwatch >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		if (!outer.destroying)
		{
			Util.PlaySound(endSoundString, base.gameObject);
			CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
			modelTransform = GetModelTransform();
			if ((bool)modelTransform)
			{
				TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
				temporaryOverlayInstance.duration = 0.6f;
				temporaryOverlayInstance.animateShaderAlpha = true;
				temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlayInstance.destroyComponentOnEnd = true;
				temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashBright");
				temporaryOverlayInstance.AddToCharacterModel(modelTransform.GetComponent<CharacterModel>());
				TemporaryOverlayInstance temporaryOverlayInstance2 = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
				temporaryOverlayInstance2.duration = 0.7f;
				temporaryOverlayInstance2.animateShaderAlpha = true;
				temporaryOverlayInstance2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlayInstance2.destroyComponentOnEnd = true;
				temporaryOverlayInstance2.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashExpanded");
				temporaryOverlayInstance2.AddToCharacterModel(modelTransform.GetComponent<CharacterModel>());
			}
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
		if ((bool)base.characterMotor)
		{
			base.characterMotor.disableAirControlUntilCollision = false;
		}
		base.OnExit();
	}
}

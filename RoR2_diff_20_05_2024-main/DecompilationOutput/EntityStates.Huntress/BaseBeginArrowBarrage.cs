using RoR2;
using UnityEngine;

namespace EntityStates.Huntress;

public class BaseBeginArrowBarrage : BaseState
{
	private Transform modelTransform;

	[SerializeField]
	public float basePrepDuration;

	[SerializeField]
	public float blinkDuration = 0.3f;

	[SerializeField]
	public float jumpCoefficient = 25f;

	public static GameObject blinkPrefab;

	public static string blinkSoundString;

	[SerializeField]
	public Vector3 blinkVector;

	private Vector3 worldBlinkVector;

	private float prepDuration;

	private bool beginBlink;

	private CharacterModel characterModel;

	private HurtBoxGroup hurtboxGroup;

	protected CameraTargetParams.AimRequest aimRequest;

	private static int BeginArrowRainStateHash = Animator.StringToHash("BeginArrowRain");

	private static int BeginArrowRainParamHash = Animator.StringToHash("BeginArrowRain.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(blinkSoundString, base.gameObject);
		modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			characterModel = modelTransform.GetComponent<CharacterModel>();
			hurtboxGroup = modelTransform.GetComponent<HurtBoxGroup>();
		}
		prepDuration = basePrepDuration / attackSpeedStat;
		PlayAnimation("FullBody, Override", BeginArrowRainStateHash, BeginArrowRainParamHash, prepDuration);
		if ((bool)base.characterMotor)
		{
			base.characterMotor.velocity = Vector3.zero;
		}
		if ((bool)base.cameraTargetParams)
		{
			aimRequest = base.cameraTargetParams.RequestAimType(CameraTargetParams.AimType.Aura);
		}
		Vector3 direction = GetAimRay().direction;
		direction.y = 0f;
		direction.Normalize();
		Vector3 up = Vector3.up;
		worldBlinkVector = Matrix4x4.TRS(base.transform.position, Util.QuaternionSafeLookRotation(direction, up), new Vector3(1f, 1f, 1f)).MultiplyPoint3x4(blinkVector) - base.transform.position;
		worldBlinkVector.Normalize();
	}

	private void CreateBlinkEffect(Vector3 origin)
	{
		EffectData effectData = new EffectData();
		effectData.rotation = Util.QuaternionSafeLookRotation(worldBlinkVector);
		effectData.origin = origin;
		EffectManager.SpawnEffect(blinkPrefab, effectData, transmit: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= prepDuration && !beginBlink)
		{
			beginBlink = true;
			CreateBlinkEffect(base.transform.position);
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
		}
		if (beginBlink && (bool)base.characterMotor)
		{
			base.characterMotor.velocity = Vector3.zero;
			base.characterMotor.rootMotion += worldBlinkVector * (base.characterBody.jumpPower * jumpCoefficient * GetDeltaTime());
		}
		if (base.fixedAge >= blinkDuration + prepDuration && base.isAuthority)
		{
			outer.SetNextState(InstantiateNextState());
		}
	}

	protected virtual EntityState InstantiateNextState()
	{
		return null;
	}

	public override void OnExit()
	{
		CreateBlinkEffect(base.transform.position);
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
		aimRequest?.Dispose();
		base.OnExit();
	}
}

using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Merc;

public class EvisDash : BaseState
{
	private Transform modelTransform;

	public static GameObject blinkPrefab;

	private float stopwatch;

	private Vector3 dashVector = Vector3.zero;

	public static float smallHopVelocity;

	public static float dashPrepDuration;

	public static float dashDuration = 0.3f;

	public static float speedCoefficient = 25f;

	public static string beginSoundString;

	public static string endSoundString;

	public static float overlapSphereRadius;

	public static float lollypopFactor;

	private Animator animator;

	private CharacterModel characterModel;

	private HurtBoxGroup hurtboxGroup;

	private bool isDashing;

	private CameraTargetParams.AimRequest aimRequest;

	private static int EvisPrepStateHash = Animator.StringToHash("EvisPrep");

	private static int EvisPrepParamHash = Animator.StringToHash("EvisPrep.playbackRate");

	private static int EvisLoopExitStateHash = Animator.StringToHash("EvisLoopExit");

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(beginSoundString, base.gameObject);
		modelTransform = GetModelTransform();
		if ((bool)base.cameraTargetParams)
		{
			aimRequest = base.cameraTargetParams.RequestAimType(CameraTargetParams.AimType.Aura);
		}
		if ((bool)modelTransform)
		{
			animator = modelTransform.GetComponent<Animator>();
			characterModel = modelTransform.GetComponent<CharacterModel>();
		}
		if (base.isAuthority)
		{
			SmallHop(base.characterMotor, smallHopVelocity);
		}
		if (NetworkServer.active)
		{
			base.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
		}
		PlayAnimation("FullBody, Override", EvisPrepStateHash, EvisPrepParamHash, dashPrepDuration);
		dashVector = base.inputBank.aimDirection;
		base.characterDirection.forward = dashVector;
	}

	private void CreateBlinkEffect(Vector3 origin)
	{
		EffectData effectData = new EffectData();
		effectData.rotation = Util.QuaternionSafeLookRotation(dashVector);
		effectData.origin = origin;
		EffectManager.SpawnEffect(blinkPrefab, effectData, transmit: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		stopwatch += GetDeltaTime();
		if (stopwatch > dashPrepDuration && !isDashing)
		{
			isDashing = true;
			dashVector = base.inputBank.aimDirection;
			CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
			PlayCrossfade("FullBody, Override", "EvisLoop", 0.1f);
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
		bool flag = stopwatch >= dashDuration + dashPrepDuration;
		if (isDashing)
		{
			if ((bool)base.characterMotor && (bool)base.characterDirection)
			{
				base.characterMotor.rootMotion += dashVector * (moveSpeedStat * speedCoefficient * GetDeltaTime());
			}
			if (base.isAuthority)
			{
				Collider[] colliders;
				int num = HGPhysics.OverlapSphere(out colliders, base.transform.position, base.characterBody.radius + overlapSphereRadius * (flag ? lollypopFactor : 1f), LayerIndex.entityPrecise.mask);
				for (int i = 0; i < num; i++)
				{
					HurtBox component = colliders[i].GetComponent<HurtBox>();
					if ((bool)component && component.healthComponent != base.healthComponent)
					{
						Evis nextState = new Evis();
						outer.SetNextState(nextState);
						break;
					}
				}
				HGPhysics.ReturnResults(colliders);
			}
		}
		if (flag && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		Util.PlaySound(endSoundString, base.gameObject);
		base.characterMotor.velocity *= 0.1f;
		SmallHop(base.characterMotor, smallHopVelocity);
		aimRequest?.Dispose();
		PlayAnimation("FullBody, Override", EvisLoopExitStateHash);
		if (NetworkServer.active)
		{
			base.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
		}
		base.OnExit();
	}
}

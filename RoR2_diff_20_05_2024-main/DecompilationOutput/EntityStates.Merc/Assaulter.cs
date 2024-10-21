using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Merc;

public class Assaulter : BaseState
{
	private Transform modelTransform;

	public static GameObject dashPrefab;

	public static float smallHopVelocity;

	public static float dashPrepDuration;

	public static float dashDuration = 0.3f;

	public static float speedCoefficient = 25f;

	public static string beginSoundString;

	public static string endSoundString;

	public static float damageCoefficient;

	public static float procCoefficient;

	public static GameObject hitEffectPrefab;

	public static float hitPauseDuration;

	private float stopwatch;

	private Vector3 dashVector = Vector3.zero;

	private Animator animator;

	private CharacterModel characterModel;

	private HurtBoxGroup hurtboxGroup;

	private OverlapAttack overlapAttack;

	private ChildLocator childLocator;

	private bool isDashing;

	private bool inHitPause;

	private float hitPauseTimer;

	private CameraTargetParams.AimRequest aimRequest;

	private int originalLayer;

	private static int AssaulterPrepStateHash = Animator.StringToHash("AssaulterPrep");

	private static int AssaulterPrepParamHash = Animator.StringToHash("AssaulterPrep.playbackRate");

	private static int EvisLoopExitStateHash = Animator.StringToHash("EvisLoopExit");

	public bool hasHit { get; private set; }

	public int dashIndex { private get; set; }

	public override void Reset()
	{
		base.Reset();
		modelTransform = null;
		stopwatch = 0f;
		dashVector = Vector3.zero;
		animator = null;
		characterModel = null;
		hurtboxGroup = null;
		if (overlapAttack != null)
		{
			overlapAttack.Reset();
		}
		childLocator = null;
		isDashing = false;
		inHitPause = false;
		hitPauseTimer = 0f;
		hasHit = false;
		dashIndex = 0;
	}

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
			childLocator = modelTransform.GetComponent<ChildLocator>();
			hurtboxGroup = modelTransform.GetComponent<HurtBoxGroup>();
			if ((bool)childLocator)
			{
				childLocator.FindChild("PreDashEffect").gameObject.SetActive(value: true);
			}
		}
		SmallHop(base.characterMotor, smallHopVelocity);
		PlayAnimation("FullBody, Override", AssaulterPrepStateHash, AssaulterPrepParamHash, dashPrepDuration);
		dashVector = base.inputBank.aimDirection;
		overlapAttack = InitMeleeOverlap(damageCoefficient, hitEffectPrefab, modelTransform, "Assaulter");
		overlapAttack.damageType = DamageType.Stun1s;
		if (NetworkServer.active)
		{
			base.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility.buffIndex);
		}
	}

	private void CreateDashEffect()
	{
		Transform transform = childLocator.FindChild("DashCenter");
		if ((bool)transform && (bool)dashPrefab)
		{
			if (!EffectManager.ShouldUsePooledEffect(dashPrefab))
			{
				Object.Instantiate(dashPrefab, transform.position, Util.QuaternionSafeLookRotation(dashVector), transform);
			}
			else
			{
				EffectManager.GetAndActivatePooledEffect(dashPrefab, transform.position, Util.QuaternionSafeLookRotation(dashVector), transform);
			}
		}
		if ((bool)childLocator)
		{
			childLocator.FindChild("PreDashEffect").gameObject.SetActive(value: false);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		base.characterDirection.forward = dashVector;
		if (stopwatch > dashPrepDuration / attackSpeedStat && !isDashing)
		{
			isDashing = true;
			dashVector = base.inputBank.aimDirection;
			CreateDashEffect();
			PlayCrossfade("FullBody, Override", "AssaulterLoop", 0.1f);
			originalLayer = base.gameObject.layer;
			base.gameObject.layer = LayerIndex.GetAppropriateFakeLayerForTeam(base.teamComponent.teamIndex).intVal;
			base.characterMotor.Motor.RebuildCollidableLayers();
			if ((bool)modelTransform)
			{
				TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
				temporaryOverlayInstance.duration = 0.7f;
				temporaryOverlayInstance.animateShaderAlpha = true;
				temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlayInstance.destroyComponentOnEnd = true;
				temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matMercEnergized");
				temporaryOverlayInstance.AddToCharacterModel(modelTransform.GetComponent<CharacterModel>());
			}
		}
		float deltaTime = GetDeltaTime();
		if (!isDashing)
		{
			stopwatch += deltaTime;
		}
		else if (base.isAuthority)
		{
			base.characterMotor.velocity = Vector3.zero;
			if (!inHitPause)
			{
				bool num = overlapAttack.Fire();
				stopwatch += deltaTime;
				if (num)
				{
					if (!hasHit)
					{
						hasHit = true;
					}
					inHitPause = true;
					hitPauseTimer = hitPauseDuration / attackSpeedStat;
					if ((bool)modelTransform)
					{
						TemporaryOverlayInstance temporaryOverlayInstance2 = TemporaryOverlayManager.AddOverlay(modelTransform.gameObject);
						temporaryOverlayInstance2.duration = hitPauseDuration / attackSpeedStat;
						temporaryOverlayInstance2.animateShaderAlpha = true;
						temporaryOverlayInstance2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
						temporaryOverlayInstance2.destroyComponentOnEnd = true;
						temporaryOverlayInstance2.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matMercEvisTarget");
						temporaryOverlayInstance2.AddToCharacterModel(modelTransform.GetComponent<CharacterModel>());
					}
				}
				base.characterMotor.rootMotion += dashVector * moveSpeedStat * speedCoefficient * deltaTime;
			}
			else
			{
				hitPauseTimer -= deltaTime;
				if (hitPauseTimer < 0f)
				{
					inHitPause = false;
				}
			}
		}
		if (stopwatch >= dashDuration + dashPrepDuration / attackSpeedStat && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override void OnExit()
	{
		base.gameObject.layer = originalLayer;
		base.characterMotor.Motor.RebuildCollidableLayers();
		Util.PlaySound(endSoundString, base.gameObject);
		if (base.isAuthority)
		{
			base.characterMotor.velocity *= 0.1f;
			SmallHop(base.characterMotor, smallHopVelocity);
		}
		aimRequest?.Dispose();
		if ((bool)childLocator)
		{
			childLocator.FindChild("PreDashEffect").gameObject.SetActive(value: false);
		}
		PlayAnimation("FullBody, Override", EvisLoopExitStateHash);
		if (NetworkServer.active)
		{
			base.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility.buffIndex);
		}
		base.OnExit();
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write((byte)dashIndex);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		dashIndex = reader.ReadByte();
	}
}

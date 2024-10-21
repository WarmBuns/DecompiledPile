using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.FalseSon;

public class StepBrothers : BaseState
{
	private Transform modelTransform;

	public static GameObject dashPrefab;

	public static float smallHopVelocity;

	public static float dashPrepDuration;

	public static float dashDuration = 0.3f;

	public static float speedCoefficient = 25f;

	public static string beginSoundString;

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

	private CharacterBody survivorBody;

	public static GameObject explosionEffect;

	public BlastAttack explosionAttack;

	private int originalLayer;

	public bool hasHit { get; private set; }

	public int dashIndex { private get; set; }

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(beginSoundString, base.gameObject);
		modelTransform = GetModelTransform();
		survivorBody = base.gameObject.GetComponent<CharacterBody>();
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
			_ = (bool)childLocator;
		}
		PlayAnimation("FullBody, Override", "StepBrothersPrep", "StepBrothersPrep.playbackRate", dashPrepDuration);
		dashVector = base.inputBank.aimDirection;
		overlapAttack = InitMeleeOverlap(0f, hitEffectPrefab, modelTransform, "StepBrothers");
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
			Object.Instantiate(dashPrefab, transform.position, Util.QuaternionSafeLookRotation(dashVector), transform);
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
			PlayCrossfade("FullBody, Override", "StepBrothersLoop", 0.1f);
			originalLayer = base.gameObject.layer;
			base.gameObject.layer = LayerIndex.GetAppropriateFakeLayerForTeam(base.teamComponent.teamIndex).intVal;
			base.characterMotor.Motor.RebuildCollidableLayers();
		}
		if (!isDashing)
		{
			stopwatch += GetDeltaTime();
		}
		else if (base.isAuthority)
		{
			base.characterMotor.velocity = Vector3.zero;
			if (!inHitPause)
			{
				bool num = overlapAttack.Fire();
				stopwatch += GetDeltaTime();
				if (num)
				{
					Vector3 position = base.gameObject.transform.position;
					Object.Instantiate(explosionEffect, position, Quaternion.identity);
					explosionAttack = new BlastAttack();
					explosionAttack.attacker = base.gameObject;
					explosionAttack.inflictor = base.gameObject;
					explosionAttack.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
					explosionAttack.baseDamage = damageStat * damageCoefficient;
					explosionAttack.position = position;
					explosionAttack.radius = 20f;
					explosionAttack.Fire();
					if (!hasHit)
					{
						hasHit = true;
					}
					inHitPause = true;
					hitPauseTimer = hitPauseDuration / attackSpeedStat;
				}
				base.characterMotor.rootMotion += dashVector * moveSpeedStat * speedCoefficient * GetDeltaTime();
			}
			else
			{
				hitPauseTimer -= GetDeltaTime();
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
		_ = base.isAuthority;
		aimRequest?.Dispose();
		_ = (bool)childLocator;
		PlayAnimation("FullBody, Override", "StepBrothersLoopExit");
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

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

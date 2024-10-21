using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Croco;

public class BaseLeap : BaseCharacterMain
{
	public static float minimumDuration;

	public static float blastRadius;

	public static float blastProcCoefficient;

	[SerializeField]
	public float blastDamageCoefficient;

	[SerializeField]
	public float blastForce;

	public static string leapSoundString;

	public static GameObject projectilePrefab;

	[SerializeField]
	public Vector3 blastBonusForce;

	[SerializeField]
	public GameObject blastImpactEffectPrefab;

	[SerializeField]
	public GameObject blastEffectPrefab;

	public static float airControl;

	public static float aimVelocity;

	public static float upwardVelocity;

	public static float forwardVelocity;

	public static float minimumY;

	public static float minYVelocityForAnim;

	public static float maxYVelocityForAnim;

	public static float knockbackForce;

	[SerializeField]
	public GameObject fistEffectPrefab;

	public static string soundLoopStartEvent;

	public static string soundLoopStopEvent;

	public static NetworkSoundEventDef landingSound;

	private float previousAirControl;

	private GameObject leftFistEffectInstance;

	private GameObject rightFistEffectInstance;

	protected bool isCritAuthority;

	protected CrocoDamageTypeController crocoDamageTypeController;

	private bool detonateNextFrame;

	private static int LightImpactStateHash = Animator.StringToHash("LightImpact");

	private static int BufferEmptyStateHash = Animator.StringToHash("BufferEmpty");

	protected virtual DamageTypeCombo GetBlastDamageType()
	{
		return DamageType.Generic;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		crocoDamageTypeController = GetComponent<CrocoDamageTypeController>();
		previousAirControl = base.characterMotor.airControl;
		base.characterMotor.airControl = airControl;
		Vector3 direction = GetAimRay().direction;
		if (base.isAuthority)
		{
			base.characterBody.isSprinting = true;
			direction.y = Mathf.Max(direction.y, minimumY);
			Vector3 vector = direction.normalized * aimVelocity * moveSpeedStat;
			Vector3 vector2 = Vector3.up * upwardVelocity;
			Vector3 vector3 = new Vector3(direction.x, 0f, direction.z).normalized * forwardVelocity;
			base.characterMotor.Motor.ForceUnground();
			base.characterMotor.velocity = vector + vector2 + vector3;
			isCritAuthority = RollCrit();
		}
		base.characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
		GetModelTransform().GetComponent<AimAnimator>().enabled = true;
		PlayCrossfade("Gesture, Override", "Leap", 0.1f);
		PlayCrossfade("Gesture, AdditiveHigh", "Leap", 0.1f);
		PlayCrossfade("Gesture, Override", "Leap", 0.1f);
		Util.PlaySound(leapSoundString, base.gameObject);
		base.characterDirection.moveVector = direction;
		leftFistEffectInstance = Object.Instantiate(fistEffectPrefab, FindModelChild("MuzzleHandL"));
		rightFistEffectInstance = Object.Instantiate(fistEffectPrefab, FindModelChild("MuzzleHandR"));
		if (base.isAuthority)
		{
			base.characterMotor.onMovementHit += OnMovementHit;
		}
		Util.PlaySound(soundLoopStartEvent, base.gameObject);
	}

	private void OnMovementHit(ref CharacterMotor.MovementHitInfo movementHitInfo)
	{
		detonateNextFrame = true;
	}

	private void onHitGroundServer(ref CharacterMotor.HitGroundInfo hitGroundInfo)
	{
		detonateNextFrame = true;
	}

	protected override void UpdateAnimationParameters()
	{
		base.UpdateAnimationParameters();
		float value = Mathf.Clamp01(Util.Remap(base.estimatedVelocity.y, minYVelocityForAnim, maxYVelocityForAnim, 0f, 1f)) * 0.97f;
		base.modelAnimator.SetFloat("LeapCycle", value, 0.1f, Time.deltaTime);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && (bool)base.characterMotor)
		{
			base.characterMotor.moveDirection = base.inputBank.moveVector;
			if (base.fixedAge >= minimumDuration && (detonateNextFrame || (base.characterMotor.Motor.GroundingStatus.IsStableOnGround && !base.characterMotor.Motor.LastGroundingStatus.IsStableOnGround)))
			{
				DoImpactAuthority();
				outer.SetNextStateToMain();
			}
		}
	}

	protected virtual void DoImpactAuthority()
	{
		if ((bool)landingSound)
		{
			EffectManager.SimpleSoundEffect(landingSound.index, base.characterBody.footPosition, transmit: true);
		}
	}

	protected BlastAttack.Result DetonateAuthority()
	{
		Vector3 footPosition = base.characterBody.footPosition;
		EffectManager.SpawnEffect(blastEffectPrefab, new EffectData
		{
			origin = footPosition,
			scale = blastRadius
		}, transmit: true);
		return new BlastAttack
		{
			attacker = base.gameObject,
			baseDamage = damageStat * blastDamageCoefficient,
			baseForce = blastForce,
			bonusForce = blastBonusForce,
			crit = isCritAuthority,
			damageType = GetBlastDamageType(),
			falloffModel = BlastAttack.FalloffModel.None,
			procCoefficient = blastProcCoefficient,
			radius = blastRadius,
			position = footPosition,
			attackerFiltering = AttackerFiltering.NeverHitSelf,
			impactEffect = EffectCatalog.FindEffectIndexFromPrefab(blastImpactEffectPrefab),
			teamIndex = base.teamComponent.teamIndex
		}.Fire();
	}

	protected void DropAcidPoolAuthority()
	{
		Vector3 footPosition = base.characterBody.footPosition;
		FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
		fireProjectileInfo.projectilePrefab = projectilePrefab;
		fireProjectileInfo.crit = isCritAuthority;
		fireProjectileInfo.force = 0f;
		fireProjectileInfo.damage = damageStat;
		fireProjectileInfo.owner = base.gameObject;
		fireProjectileInfo.rotation = Quaternion.identity;
		fireProjectileInfo.position = footPosition;
		FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
		ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
	}

	public override void OnExit()
	{
		Util.PlaySound(soundLoopStopEvent, base.gameObject);
		if (base.isAuthority)
		{
			base.characterMotor.onMovementHit -= OnMovementHit;
		}
		base.characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
		base.characterMotor.airControl = previousAirControl;
		base.characterBody.isSprinting = false;
		int layerIndex = base.modelAnimator.GetLayerIndex("Impact");
		if (layerIndex >= 0)
		{
			base.modelAnimator.SetLayerWeight(layerIndex, 2f);
			PlayAnimation("Impact", LightImpactStateHash);
		}
		PlayCrossfade("Gesture, Override", BufferEmptyStateHash, 0.1f);
		PlayCrossfade("Gesture, AdditiveHigh", BufferEmptyStateHash, 0.1f);
		EntityState.Destroy(leftFistEffectInstance);
		EntityState.Destroy(rightFistEffectInstance);
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

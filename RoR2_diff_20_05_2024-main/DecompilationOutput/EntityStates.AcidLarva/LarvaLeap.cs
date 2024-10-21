using System.Linq;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.AcidLarva;

public class LarvaLeap : BaseCharacterMain
{
	[SerializeField]
	public float minimumDuration;

	[SerializeField]
	public float blastRadius;

	[SerializeField]
	public float blastProcCoefficient;

	[SerializeField]
	public float blastDamageCoefficient;

	[SerializeField]
	public float blastForce;

	[SerializeField]
	public string leapSoundString;

	[SerializeField]
	public GameObject projectilePrefab;

	[SerializeField]
	public Vector3 blastBonusForce;

	[SerializeField]
	public GameObject blastImpactEffectPrefab;

	[SerializeField]
	public GameObject blastEffectPrefab;

	[SerializeField]
	public float airControl;

	[SerializeField]
	public float aimVelocity;

	[SerializeField]
	public float upwardVelocity;

	[SerializeField]
	public float forwardVelocity;

	[SerializeField]
	public float minimumY;

	[SerializeField]
	public float minYVelocityForAnim;

	[SerializeField]
	public float maxYVelocityForAnim;

	[SerializeField]
	public float knockbackForce;

	[SerializeField]
	public float maxRadiusToConfirmDetonate;

	[SerializeField]
	public bool confirmDetonate;

	[SerializeField]
	public GameObject spinEffectPrefab;

	[SerializeField]
	public string spinEffectMuzzleString;

	[SerializeField]
	public string soundLoopStartEvent;

	[SerializeField]
	public string soundLoopStopEvent;

	[SerializeField]
	public NetworkSoundEventDef landingSound;

	[SerializeField]
	public float detonateSelfDamageFraction;

	private float previousAirControl;

	private GameObject spinEffectInstance;

	protected bool isCritAuthority;

	protected CrocoDamageTypeController crocoDamageTypeController;

	private bool detonateNextFrame;

	private static int EmptyStateHash = Animator.StringToHash("Empty");

	protected virtual DamageTypeCombo GetBlastDamageType()
	{
		return DamageType.Generic;
	}

	public override void OnEnter()
	{
		base.OnEnter();
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
		PlayCrossfade("Gesture, Override", "LarvaLeap", 0.1f);
		Util.PlaySound(leapSoundString, base.gameObject);
		base.characterDirection.moveVector = direction;
		spinEffectInstance = Object.Instantiate(spinEffectPrefab, FindModelChild(spinEffectMuzzleString));
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

	protected override void UpdateAnimationParameters()
	{
		base.UpdateAnimationParameters();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!base.isAuthority || !base.characterMotor)
		{
			return;
		}
		base.characterMotor.moveDirection = base.inputBank.moveVector;
		base.characterDirection.moveVector = base.characterMotor.velocity;
		base.characterMotor.disableAirControlUntilCollision = base.characterMotor.velocity.y < 0f;
		if (base.fixedAge >= minimumDuration && (detonateNextFrame || (base.characterMotor.Motor.GroundingStatus.IsStableOnGround && !base.characterMotor.Motor.LastGroundingStatus.IsStableOnGround)))
		{
			bool flag = true;
			if (confirmDetonate)
			{
				BullseyeSearch bullseyeSearch = new BullseyeSearch();
				bullseyeSearch.viewer = base.characterBody;
				bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
				bullseyeSearch.teamMaskFilter.RemoveTeam(base.characterBody.teamComponent.teamIndex);
				bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
				bullseyeSearch.minDistanceFilter = 0f;
				bullseyeSearch.maxDistanceFilter = maxRadiusToConfirmDetonate;
				bullseyeSearch.searchOrigin = base.inputBank.aimOrigin;
				bullseyeSearch.searchDirection = base.inputBank.aimDirection;
				bullseyeSearch.maxAngleFilter = 180f;
				bullseyeSearch.filterByLoS = false;
				bullseyeSearch.RefreshCandidates();
				flag = bullseyeSearch.GetResults().FirstOrDefault();
			}
			if (flag)
			{
				DoImpactAuthority();
			}
			outer.SetNextStateToMain();
		}
	}

	protected virtual void DoImpactAuthority()
	{
		DetonateAuthority();
		if ((bool)landingSound)
		{
			EffectManager.SimpleSoundEffect(landingSound.index, base.characterBody.footPosition, transmit: true);
		}
		base.healthComponent.TakeDamage(new DamageInfo
		{
			damage = base.healthComponent.fullCombinedHealth * detonateSelfDamageFraction,
			attacker = base.characterBody.gameObject,
			position = base.characterBody.corePosition,
			damageType = DamageType.Generic
		});
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

	protected void FireProjectile()
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
		base.characterMotor.airControl = previousAirControl;
		base.characterBody.isSprinting = false;
		if ((bool)spinEffectInstance)
		{
			EntityState.Destroy(spinEffectInstance);
		}
		PlayAnimation("Gesture, Override", EmptyStateHash);
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}

using RoR2;
using UnityEngine;

namespace EntityStates.FalseSon;

public class ClubGroundSlam : BaseState
{
	public float charge;

	public static float airControl;

	public static float minimumDuration;

	public static float blastRadius;

	public static float blastProcCoefficient;

	public static float blastDamageCoefficient;

	public static float blastForce;

	public static string enterSoundString;

	public static float initialVerticalVelocity;

	public static float exitVerticalVelocity;

	public static float verticalAcceleration;

	public static float exitSlowdownCoefficient;

	public static Vector3 blastBonusForce;

	public static GameObject blastImpactEffectPrefab;

	public static GameObject blastEffectPrefab;

	public static float normalizedVFXPositionBetweenFootAndClub;

	public static float normalizedBlastPositionBetweenFootAndClub;

	public static float smallHopVelocity;

	public static float heightInfluencePercent;

	private float previousAirControl;

	private float timeInAir;

	private float airClubTimer;

	private bool hitTheGround;

	private float delayBeforeExit;

	private bool detonateNextFrame;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayCrossfade("Body", "GroundSlam", 0.2f);
		if (base.isAuthority)
		{
			base.characterMotor.onHitGroundAuthority += OnHitGroundAuthority;
			base.characterMotor.velocity.y = initialVerticalVelocity;
		}
		Util.PlaySound(enterSoundString, base.gameObject);
		previousAirControl = base.characterMotor.airControl;
		base.characterMotor.airControl = airControl;
		base.characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		float deltaTime = GetDeltaTime();
		airClubTimer += deltaTime;
		timeInAir = 0f;
		if (airClubTimer > 0.5f)
		{
			timeInAir = airClubTimer;
		}
		if (base.isAuthority && (bool)base.characterMotor)
		{
			base.characterMotor.moveDirection = base.inputBank.moveVector;
			base.characterDirection.moveVector = base.characterMotor.moveDirection;
			base.characterMotor.velocity.y += verticalAcceleration * deltaTime;
			if (!hitTheGround && base.fixedAge >= minimumDuration && (detonateNextFrame || (base.characterMotor.Motor.GroundingStatus.IsStableOnGround && !base.characterMotor.Motor.LastGroundingStatus.IsStableOnGround)))
			{
				PlayAnimation("Body", "GroundSlamExit");
				DetonateAuthority();
				hitTheGround = true;
				delayBeforeExit = base.fixedAge;
			}
			else if (hitTheGround && base.fixedAge >= delayBeforeExit + 0.1f)
			{
				outer.SetNextStateToMain();
			}
			else
			{
				base.characterBody.SetSpreadBloom(charge);
			}
		}
	}

	public override void OnExit()
	{
		if (!base.isAuthority)
		{
			PlayAnimation("Body", "GroundSlamExit");
		}
		base.characterBody.SetSpreadBloom(0f, canOnlyIncreaseBloom: false);
		if (base.isAuthority)
		{
			base.characterMotor.onHitGroundAuthority -= OnHitGroundAuthority;
			base.characterMotor.velocity *= exitSlowdownCoefficient;
			base.characterMotor.velocity.y = exitVerticalVelocity;
			if (!hitTheGround)
			{
				PlayAnimation("Body", "Idle");
			}
		}
		base.characterMotor.airControl = previousAirControl;
		base.characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
		SmallHop(base.characterMotor, smallHopVelocity + airClubTimer * heightInfluencePercent);
		base.OnExit();
	}

	private void OnHitGroundAuthority(ref CharacterMotor.HitGroundInfo hitGroundInfo)
	{
		detonateNextFrame = true;
	}

	protected BlastAttack.Result DetonateAuthority()
	{
		Vector3 position = FindModelChild("ClubExplosionPoint").transform.position;
		Vector3 footPosition = base.characterBody.footPosition;
		Vector3 origin = footPosition + (position - footPosition) * normalizedVFXPositionBetweenFootAndClub;
		Vector3 position2 = footPosition + (position - footPosition) * normalizedBlastPositionBetweenFootAndClub;
		EffectManager.SpawnEffect(blastEffectPrefab, new EffectData
		{
			origin = origin,
			scale = blastRadius * (charge + 1.25f + timeInAir * 2f)
		}, transmit: true);
		return new BlastAttack
		{
			attacker = base.gameObject,
			baseDamage = damageStat * (blastDamageCoefficient * charge) + (base.characterBody.maxHealth - (base.characterBody.baseMaxHealth + base.characterBody.levelMaxHealth * (float)((int)base.characterBody.level - 1))) * 0.01f,
			baseForce = blastForce,
			bonusForce = blastBonusForce,
			crit = RollCrit(),
			falloffModel = BlastAttack.FalloffModel.None,
			procCoefficient = blastProcCoefficient,
			radius = blastRadius * (charge + 1.25f + timeInAir * 2f),
			position = position2,
			attackerFiltering = AttackerFiltering.NeverHitSelf,
			impactEffect = EffectCatalog.FindEffectIndexFromPrefab(blastImpactEffectPrefab),
			teamIndex = base.teamComponent.teamIndex
		}.Fire();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}

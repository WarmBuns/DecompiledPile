using RoR2;
using UnityEngine;

namespace EntityStates.Loader;

public class GroundSlam : BaseCharacterMain
{
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

	public static GameObject fistEffectPrefab;

	private float previousAirControl;

	private GameObject leftFistEffectInstance;

	private GameObject rightFistEffectInstance;

	private bool detonateNextFrame;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayCrossfade("Body", "GroundSlam", 0.2f);
		if (base.isAuthority)
		{
			base.characterMotor.onMovementHit += OnMovementHit;
			base.characterMotor.velocity.y = initialVerticalVelocity;
		}
		Util.PlaySound(enterSoundString, base.gameObject);
		previousAirControl = base.characterMotor.airControl;
		base.characterMotor.airControl = airControl;
		leftFistEffectInstance = Object.Instantiate(fistEffectPrefab, FindModelChild("MechHandR"));
		rightFistEffectInstance = Object.Instantiate(fistEffectPrefab, FindModelChild("MechHandL"));
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && (bool)base.characterMotor)
		{
			base.characterMotor.moveDirection = base.inputBank.moveVector;
			base.characterDirection.moveVector = base.characterMotor.moveDirection;
			base.characterMotor.velocity.y += verticalAcceleration * GetDeltaTime();
			if (base.fixedAge >= minimumDuration && (detonateNextFrame || base.characterMotor.Motor.GroundingStatus.IsStableOnGround))
			{
				DetonateAuthority();
				outer.SetNextStateToMain();
			}
		}
	}

	public override void OnExit()
	{
		if (base.isAuthority)
		{
			base.characterMotor.onMovementHit -= OnMovementHit;
			base.characterMotor.Motor.ForceUnground();
			base.characterMotor.velocity *= exitSlowdownCoefficient;
			base.characterMotor.velocity.y = exitVerticalVelocity;
		}
		base.characterMotor.airControl = previousAirControl;
		EntityState.Destroy(leftFistEffectInstance);
		EntityState.Destroy(rightFistEffectInstance);
		base.OnExit();
	}

	private void OnMovementHit(ref CharacterMotor.MovementHitInfo movementHitInfo)
	{
		detonateNextFrame = true;
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
			crit = RollCrit(),
			damageType = DamageType.Stun1s,
			falloffModel = BlastAttack.FalloffModel.None,
			procCoefficient = blastProcCoefficient,
			radius = blastRadius,
			position = footPosition,
			attackerFiltering = AttackerFiltering.NeverHitSelf,
			impactEffect = EffectCatalog.FindEffectIndexFromPrefab(blastImpactEffectPrefab),
			teamIndex = base.teamComponent.teamIndex
		}.Fire();
	}
}

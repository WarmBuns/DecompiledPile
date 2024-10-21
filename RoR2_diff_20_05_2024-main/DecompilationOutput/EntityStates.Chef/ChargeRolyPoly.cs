using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Chef;

public class ChargeRolyPoly : BaseSkillState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public float baseChargeDuration = 1f;

	[SerializeField]
	public float minChargeForChargedAttack = 0.1f;

	[SerializeField]
	public float walkSpeedCoefficient;

	[SerializeField]
	public GameObject skiddingVFX1;

	[SerializeField]
	public string chargeLevelSFX1;

	[SerializeField]
	public string chargeLevelSFX2;

	[SerializeField]
	public string chargeLevelSFX3;

	[SerializeField]
	public float explosionDmgCoefficient;

	[SerializeField]
	public float dmgIncreasePercent;

	[SerializeField]
	public float explosionForce;

	[SerializeField]
	public Vector3 extraExplosionForce;

	[SerializeField]
	public float explosionRadius;

	[SerializeField]
	public GameObject explosionPrefab1;

	[SerializeField]
	public GameObject explosionPrefab2;

	[SerializeField]
	public GameObject explosionPrefab3;

	[SerializeField]
	public float explodeRadiusVFX;

	private float duration;

	private GameObject explosionPrefab;

	private int gearShifLevel;

	private GameObject chargeLevelVFXBase;

	private bool chargeAnimPlayed;

	private float gearToChargeProgress = 0.3f;

	private bool obtainedGear1;

	private bool obtainedGear2;

	private bool obtainedGear3;

	private Transform modelTransform;

	private EffectManagerHelper rolyPolyChargeFXReference;

	private bool hasBoost;

	private bool useRootMotion;

	private bool hasCharacterMotor;

	private bool hasRailMotor;

	private bool hasCharacterDirection;

	private bool hasAimAnimator;

	private AimAnimator aimAnimator;

	private Vector3 moveVector = Vector3.zero;

	private Vector3 aimDirection = Vector3.forward;

	private ChefController chefController;

	protected float charge { get; private set; }

	protected float chargeDuration { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			chefController = base.characterBody.GetComponent<ChefController>();
			if (chefController != null)
			{
				chefController.blockOtherSkills = true;
			}
			EntityStateMachine.FindByCustomName(base.gameObject, "Weapon").SetNextState(new RolyPolyWeaponBlockingState());
		}
		bool active = NetworkServer.active;
		chargeDuration = baseChargeDuration / attackSpeedStat;
		if (base.characterBody.HasBuff(DLC2Content.Buffs.Boosted))
		{
			hasBoost = true;
			if (active)
			{
				base.characterBody.RemoveBuff(DLC2Content.Buffs.Boosted);
			}
		}
		if (active)
		{
			base.characterBody.AddBuff(RoR2Content.Buffs.ArmorBoost);
		}
		base.characterMotor.walkSpeedPenaltyCoefficient = walkSpeedCoefficient;
		PlayAnimation("Body", "ChargeRolyPoly", "ChargeRolyPoly.playbackRate", 1f);
		new EffectData
		{
			origin = base.transform.position,
			scale = 1f
		};
		modelTransform = GetModelTransform();
		ChildLocator component = modelTransform.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			Transform inTransform = component.FindChild("Base");
			rolyPolyChargeFXReference = EffectManager.GetAndActivatePooledEffect(skiddingVFX1, inTransform, inResetLocal: true);
		}
		useRootMotion = ((bool)base.characterBody && base.characterBody.rootMotionInMainState && base.isGrounded) || (bool)base.railMotor;
		hasCharacterMotor = base.characterMotor;
		hasRailMotor = base.railMotor;
		hasCharacterDirection = base.characterDirection;
		Util.PlaySound("Play_chef_skill3_charge_start", base.gameObject);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		charge = Mathf.Clamp01(base.fixedAge / chargeDuration);
		base.characterBody.SetSpreadBloom(charge);
		base.characterBody.SetAimTimer(3f);
		if (charge >= minChargeForChargedAttack && charge != 1f && charge >= gearToChargeProgress)
		{
			gearToChargeProgress += 0.3f;
			explosionDmgCoefficient += explosionDmgCoefficient * dmgIncreasePercent;
			GearShift();
		}
		if (base.isAuthority)
		{
			AuthorityFixedUpdate();
		}
	}

	private void GearShift()
	{
		switch (gearShifLevel)
		{
		case 0:
			explosionPrefab = explosionPrefab1;
			Util.PlaySound(chargeLevelSFX1, base.gameObject);
			break;
		case 1:
			explosionPrefab = explosionPrefab2;
			Util.PlaySound(chargeLevelSFX2, base.gameObject);
			break;
		case 2:
			explosionPrefab = explosionPrefab3;
			Util.PlaySound(chargeLevelSFX3, base.gameObject);
			break;
		}
		EffectManager.SpawnEffect(explosionPrefab, new EffectData
		{
			origin = base.characterBody.footPosition,
			scale = explosionRadius * explodeRadiusVFX
		}, transmit: true);
		gearShifLevel++;
		BlastAttack blastAttack = new BlastAttack();
		blastAttack.attacker = base.characterBody.gameObject;
		blastAttack.baseDamage = explosionDmgCoefficient * base.characterBody.damage;
		blastAttack.baseForce = explosionForce;
		blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
		blastAttack.crit = base.characterBody.RollCrit();
		blastAttack.damageColorIndex = DamageColorIndex.Item;
		blastAttack.damageType = DamageType.SlowOnHit;
		blastAttack.inflictor = base.gameObject;
		blastAttack.position = base.transform.position;
		blastAttack.procChainMask = default(ProcChainMask);
		blastAttack.procCoefficient = 1f;
		blastAttack.radius = explosionRadius;
		blastAttack.teamIndex = base.characterBody.teamComponent.teamIndex;
		blastAttack.bonusForce = extraExplosionForce;
		blastAttack.Fire();
	}

	private void AuthorityFixedUpdate()
	{
		if (base.inputBank.skill3.justReleased || charge > 0.9f)
		{
			if (charge >= 0.9f)
			{
				charge = 3f;
			}
			else if (charge >= 0.6f)
			{
				charge = 2f;
			}
			else if (charge >= 0.3f)
			{
				charge = 1f;
			}
			else
			{
				charge = 0f;
			}
			outer.SetNextState(GetNextStateAuthority());
		}
		else
		{
			HandleRotation();
		}
	}

	private void HandleRotation()
	{
		moveVector = base.inputBank.moveVector;
		aimDirection = base.inputBank.aimDirection;
		if (useRootMotion)
		{
			if (hasCharacterMotor)
			{
				base.characterMotor.moveDirection = Vector3.zero;
			}
			if (hasRailMotor)
			{
				base.railMotor.inputMoveVector = moveVector;
			}
		}
		else
		{
			if (hasCharacterMotor)
			{
				base.characterMotor.moveDirection = moveVector;
			}
			if (hasRailMotor)
			{
				base.railMotor.inputMoveVector = moveVector;
			}
		}
		if (!hasRailMotor && hasCharacterDirection)
		{
			if (hasAimAnimator && aimAnimator.aimType == AimAnimator.AimType.Smart)
			{
				Vector3 vector = ((moveVector == Vector3.zero) ? base.characterDirection.forward : moveVector);
				float num = Vector3.Angle(aimDirection, vector);
				float num2 = Mathf.Max(aimAnimator.pitchRangeMax + aimAnimator.pitchGiveupRange, aimAnimator.yawRangeMax + aimAnimator.yawGiveupRange);
				base.characterDirection.moveVector = (((bool)base.characterBody && base.characterBody.shouldAim && num > num2) ? aimDirection : vector);
			}
			else
			{
				base.characterDirection.moveVector = (((bool)base.characterBody && base.characterBody.shouldAim) ? aimDirection : moveVector);
			}
		}
	}

	public override void OnExit()
	{
		if (rolyPolyChargeFXReference != null)
		{
			rolyPolyChargeFXReference.ReturnToPool();
			rolyPolyChargeFXReference = null;
		}
		if (NetworkServer.active)
		{
			base.characterBody.RemoveBuff(RoR2Content.Buffs.ArmorBoost);
		}
		base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
		if (base.isAuthority && chefController != null)
		{
			chefController.blockOtherSkills = hasBoost;
		}
		Util.PlaySound("Stop_chef_skill3_charge_loop", base.gameObject);
		base.OnExit();
	}

	protected virtual EntityState GetNextStateAuthority()
	{
		return new RolyPoly
		{
			charge = charge,
			hasBoost = hasBoost
		};
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}

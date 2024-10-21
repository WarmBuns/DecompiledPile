using RoR2;
using UnityEngine;

namespace EntityStates.FalseSon;

public class ChargedClubSwing : BaseState
{
	[SerializeField]
	public float baseDuration;

	public float charge;

	public bool chargeMetMinimumHold;

	public static float playerWalkSpeedCoefficient;

	public static float minimumDuration;

	public static float blastRadius;

	public static float blastProcCoefficient;

	public static float blastDamageCoefficient;

	public static float blastForce;

	public static string enterSoundString;

	public static Vector3 blastBonusForce;

	public static GameObject blastImpactEffectPrefab;

	public static GameObject blastEffectPrefab;

	public static GameObject swingEffectPrefab;

	public static float percentOfDurationToFireSwingEffect;

	public static float unchargedBlastRadius;

	public static float unchargedBlastProcCoefficient;

	public static float unchargedBlastDamageCoefficient;

	public static float unchargedBlastForce;

	public static Vector3 unchargedBlastBonusForce;

	public static GameObject unchargedSwingVFX;

	public static float percentOfAnimationToHoldPlayer;

	private GameObject swingEffectInstance;

	private bool wasSprinting;

	private bool authorityDetonated;

	private bool detonateNextFrame;

	private bool firedSwingVFX;

	public override void OnEnter()
	{
		base.OnEnter();
		baseDuration /= attackSpeedStat;
		PlayAnimation("FullBody, Override", "OverheadSwing", "ChargeSwing.playbackRate", baseDuration);
		wasSprinting = base.characterBody.isSprinting;
		base.characterMotor.walkSpeedPenaltyCoefficient = playerWalkSpeedCoefficient;
		if (chargeMetMinimumHold && base.isAuthority)
		{
			EffectData effectData = new EffectData();
			ChildLocator modelChildLocator = GetModelChildLocator();
			effectData.SetChildLocatorTransformReference(base.gameObject, modelChildLocator.FindChildIndex("OverHeadSwingPoint"));
			EffectManager.SpawnEffect(swingEffectPrefab, effectData, transmit: false);
		}
		base.characterBody.GetComponent<FalseSonController>()?.SetClubSwingAltSecondaryTimestamp();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			if (!chargeMetMinimumHold && !firedSwingVFX && base.fixedAge >= minimumDuration * percentOfDurationToFireSwingEffect)
			{
				SpawnSwingEffect();
				firedSwingVFX = true;
			}
			if (!authorityDetonated && base.fixedAge >= minimumDuration)
			{
				DetonateAuthority();
				authorityDetonated = true;
			}
			if (base.fixedAge >= baseDuration * percentOfAnimationToHoldPlayer)
			{
				outer.SetNextStateToMain();
			}
		}
	}

	private void SpawnSwingEffect()
	{
		EffectData effectData = new EffectData();
		ChildLocator modelChildLocator = GetModelChildLocator();
		effectData.SetChildLocatorTransformReference(base.gameObject, modelChildLocator.FindChildIndex("SwingVertical"));
		EffectManager.SpawnEffect(unchargedSwingVFX, effectData, transmit: true);
	}

	public override void OnExit()
	{
		base.characterMotor.walkSpeedPenaltyCoefficient = 1f;
		base.characterBody.isSprinting = wasSprinting;
		base.characterBody.SetSpreadBloom(0f, canOnlyIncreaseBloom: false);
		base.OnExit();
	}

	protected BlastAttack.Result DetonateAuthority()
	{
		BlastAttack blast = new BlastAttack();
		if (chargeMetMinimumHold)
		{
			InitializeBlastAttackAsCharged(ref blast);
		}
		else
		{
			InitializeBlastAttackNormal(ref blast);
		}
		return blast.Fire();
	}

	private void InitializeBlastAttackAsCharged(ref BlastAttack blast)
	{
		Vector3 position = FindModelChild("ClubExplosionPoint").transform.position;
		EffectManager.SpawnEffect(blastEffectPrefab, new EffectData
		{
			origin = position,
			scale = blastRadius * (charge + 1.25f)
		}, transmit: true);
		blast.attacker = base.gameObject;
		blast.baseDamage = damageStat * (blastDamageCoefficient * charge) + (base.characterBody.maxHealth - (base.characterBody.baseMaxHealth + base.characterBody.levelMaxHealth * (float)((int)base.characterBody.level - 1))) * 0.01f;
		blast.baseForce = blastForce;
		blast.bonusForce = blastBonusForce;
		blast.crit = RollCrit();
		blast.falloffModel = BlastAttack.FalloffModel.None;
		blast.procCoefficient = blastProcCoefficient;
		blast.radius = blastRadius * (charge + 1.55f);
		blast.position = position;
		blast.attackerFiltering = AttackerFiltering.NeverHitSelf;
		blast.impactEffect = EffectCatalog.FindEffectIndexFromPrefab(blastImpactEffectPrefab);
		blast.teamIndex = base.teamComponent.teamIndex;
	}

	private void InitializeBlastAttackNormal(ref BlastAttack blast)
	{
		Vector3 position = FindModelChild("ClubExplosionPoint").transform.position;
		blast.attacker = base.gameObject;
		blast.baseDamage = damageStat * (unchargedBlastDamageCoefficient * charge);
		blast.baseForce = unchargedBlastForce;
		blast.bonusForce = unchargedBlastBonusForce;
		blast.crit = RollCrit();
		blast.falloffModel = BlastAttack.FalloffModel.None;
		blast.procCoefficient = unchargedBlastProcCoefficient;
		blast.radius = unchargedBlastRadius;
		blast.position = position;
		blast.attackerFiltering = AttackerFiltering.NeverHitSelf;
		blast.impactEffect = EffectIndex.Invalid;
		blast.teamIndex = base.teamComponent.teamIndex;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}

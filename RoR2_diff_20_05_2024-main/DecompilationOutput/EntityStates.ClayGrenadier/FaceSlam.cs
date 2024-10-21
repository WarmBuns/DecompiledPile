using System.Linq;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.ClayGrenadier;

public class FaceSlam : BaseState
{
	public static float baseDuration = 3.5f;

	public static float baseDurationBeforeBlast = 1.5f;

	public static string animationLayerName = "Body";

	public static string animationStateName = "FaceSlam";

	public static string playbackRateParam = "FaceSlam.playbackRate";

	private static int FaceSlamStateHash = Animator.StringToHash("FaceSlam");

	private static int FaceSlamParamHash = Animator.StringToHash("FaceSlam.playbackRate");

	public static GameObject chargeEffectPrefab;

	public static string chargeEffectMuzzleString;

	public static GameObject blastImpactEffect;

	public static float blastDamageCoefficient = 4f;

	public static float blastForceMagnitude = 16f;

	public static float blastUpwardForce;

	public static float blastRadius = 3f;

	public static string attackSoundString;

	public static string blastMuzzleString;

	public static GameObject projectilePrefab;

	public static float projectileDamageCoefficient;

	public static float projectileForce;

	public static float projectileSnapOnAngle;

	public static float healthCostFraction;

	private BlastAttack attack;

	private Animator modelAnimator;

	private Transform modelTransform;

	private bool hasFiredBlast;

	private float duration;

	private float durationBeforeBlast;

	private GameObject chargeInstance;

	public override void OnEnter()
	{
		base.OnEnter();
		modelAnimator = GetModelAnimator();
		modelTransform = GetModelTransform();
		duration = baseDuration / attackSpeedStat;
		durationBeforeBlast = baseDurationBeforeBlast / attackSpeedStat;
		Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, attackSpeedStat);
		PlayAnimation(animationLayerName, FaceSlamStateHash, FaceSlamParamHash, duration);
		if ((bool)base.characterDirection)
		{
			base.characterDirection.moveVector = base.characterDirection.forward;
		}
		Transform transform = FindModelChild(chargeEffectMuzzleString);
		if ((bool)transform && (bool)chargeEffectPrefab)
		{
			chargeInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
			chargeInstance.transform.parent = transform;
			ScaleParticleSystemDuration component = chargeInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component)
			{
				component.newDuration = durationBeforeBlast;
			}
		}
	}

	public override void OnExit()
	{
		if ((bool)chargeInstance)
		{
			EntityState.Destroy(chargeInstance);
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > durationBeforeBlast && !hasFiredBlast)
		{
			hasFiredBlast = true;
			if ((bool)chargeInstance)
			{
				EntityState.Destroy(chargeInstance);
			}
			Vector3 footPosition = base.characterBody.footPosition;
			EffectManager.SpawnEffect(blastImpactEffect, new EffectData
			{
				origin = footPosition,
				scale = blastRadius
			}, transmit: true);
			if (NetworkServer.active && (bool)base.healthComponent)
			{
				DamageInfo damageInfo = new DamageInfo();
				damageInfo.damage = base.healthComponent.combinedHealth * healthCostFraction;
				damageInfo.position = base.characterBody.corePosition;
				damageInfo.force = Vector3.zero;
				damageInfo.damageColorIndex = DamageColorIndex.Default;
				damageInfo.crit = false;
				damageInfo.attacker = null;
				damageInfo.inflictor = null;
				damageInfo.damageType = DamageType.NonLethal | DamageType.BypassArmor;
				damageInfo.procCoefficient = 0f;
				damageInfo.procChainMask = default(ProcChainMask);
				base.healthComponent.TakeDamage(damageInfo);
			}
			if (base.isAuthority)
			{
				if ((bool)modelTransform)
				{
					Transform transform = FindModelChild(blastMuzzleString);
					if ((bool)transform)
					{
						attack = new BlastAttack();
						attack.attacker = base.gameObject;
						attack.inflictor = base.gameObject;
						attack.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
						attack.baseDamage = damageStat * blastDamageCoefficient;
						attack.baseForce = blastForceMagnitude;
						attack.position = transform.position;
						attack.radius = blastRadius;
						attack.bonusForce = new Vector3(0f, blastUpwardForce, 0f);
						attack.damageType = DamageType.ClayGoo;
						attack.Fire();
					}
				}
				Vector3 position = footPosition;
				_ = Vector3.up;
				if (Physics.Raycast(GetAimRay(), out var hitInfo, 1000f, LayerIndex.world.mask))
				{
					position = hitInfo.point;
				}
				BullseyeSearch bullseyeSearch = new BullseyeSearch();
				bullseyeSearch.viewer = base.characterBody;
				bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
				bullseyeSearch.teamMaskFilter.RemoveTeam(base.characterBody.teamComponent.teamIndex);
				bullseyeSearch.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
				bullseyeSearch.minDistanceFilter = 0f;
				bullseyeSearch.maxDistanceFilter = 1000f;
				bullseyeSearch.searchOrigin = base.inputBank.aimOrigin;
				bullseyeSearch.searchDirection = base.inputBank.aimDirection;
				bullseyeSearch.maxAngleFilter = projectileSnapOnAngle;
				bullseyeSearch.filterByLoS = false;
				bullseyeSearch.RefreshCandidates();
				HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault();
				if ((bool)hurtBox && (bool)hurtBox.healthComponent)
				{
					position = hurtBox.healthComponent.body.footPosition;
				}
				ProjectileManager.instance.FireProjectile(projectilePrefab, position, Quaternion.identity, base.gameObject, base.characterBody.damage * projectileDamageCoefficient, projectileForce, Util.CheckRoll(base.characterBody.crit, base.characterBody.master));
			}
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}

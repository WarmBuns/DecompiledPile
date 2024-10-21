using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Loader;

public class BigPunch : LoaderMeleeAttack
{
	public static int maxShockCount;

	public static float maxShockFOV;

	public static float maxShockDistance;

	public static float shockDamageCoefficient;

	public static float shockProcCoefficient;

	public static float knockbackForce;

	public static float shorthopVelocityOnEnter;

	private bool hasHit;

	private bool hasKnockbackedSelf;

	private static int BigPunchStateHash = Animator.StringToHash("BigPunch");

	private static int BigPunchParamHash = Animator.StringToHash("BigPunch.playbackRate");

	private Vector3 punchVector => base.characterDirection.forward.normalized;

	public override void OnEnter()
	{
		base.OnEnter();
		base.characterMotor.velocity.y = shorthopVelocityOnEnter;
	}

	protected override void PlayAnimation()
	{
		base.PlayAnimation();
		PlayAnimation("FullBody, Override", BigPunchStateHash, BigPunchParamHash, duration);
	}

	protected override void AuthorityFixedUpdate()
	{
		base.AuthorityFixedUpdate();
		if (hasHit && !hasKnockbackedSelf && !base.authorityInHitPause)
		{
			hasKnockbackedSelf = true;
			base.healthComponent.TakeDamageForce(punchVector * (0f - knockbackForce), alwaysApply: true);
		}
	}

	protected override void AuthorityModifyOverlapAttack(OverlapAttack overlapAttack)
	{
		base.AuthorityModifyOverlapAttack(overlapAttack);
		overlapAttack.maximumOverlapTargets = 1;
	}

	protected override void OnMeleeHitAuthority()
	{
		if (!hasHit)
		{
			base.OnMeleeHitAuthority();
			hasHit = true;
			if ((bool)FindModelChild(swingEffectMuzzleString))
			{
				FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
				fireProjectileInfo.position = GetAimRay().origin;
				fireProjectileInfo.rotation = Quaternion.LookRotation(punchVector);
				fireProjectileInfo.crit = RollCrit();
				fireProjectileInfo.damage = 1f * damageStat;
				fireProjectileInfo.owner = base.gameObject;
				fireProjectileInfo.projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LoaderZapCone");
				ProjectileManager.instance.FireProjectile(fireProjectileInfo);
			}
		}
	}

	private void FireSecondaryRaysServer()
	{
		Ray aimRay = GetAimRay();
		TeamIndex team = GetTeam();
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(team);
		bullseyeSearch.maxAngleFilter = maxShockFOV * 0.5f;
		bullseyeSearch.maxDistanceFilter = maxShockDistance;
		bullseyeSearch.searchOrigin = aimRay.origin;
		bullseyeSearch.searchDirection = punchVector;
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
		bullseyeSearch.filterByLoS = false;
		bullseyeSearch.RefreshCandidates();
		List<HurtBox> list = bullseyeSearch.GetResults().Where(Util.IsValid).ToList();
		Transform transform = FindModelChild(swingEffectMuzzleString);
		if (!transform)
		{
			return;
		}
		for (int i = 0; i < Mathf.Min(list.Count, maxShockCount); i++)
		{
			HurtBox hurtBox = list[i];
			if ((bool)hurtBox)
			{
				LightningOrb lightningOrb = new LightningOrb();
				lightningOrb.bouncedObjects = new List<HealthComponent>();
				lightningOrb.attacker = base.gameObject;
				lightningOrb.teamIndex = team;
				lightningOrb.damageValue = damageStat * shockDamageCoefficient;
				lightningOrb.isCrit = RollCrit();
				lightningOrb.origin = transform.position;
				lightningOrb.bouncesRemaining = 0;
				lightningOrb.lightningType = LightningOrb.LightningType.Loader;
				lightningOrb.procCoefficient = shockProcCoefficient;
				lightningOrb.target = hurtBox;
				OrbManager.instance.AddOrb(lightningOrb);
			}
		}
	}
}

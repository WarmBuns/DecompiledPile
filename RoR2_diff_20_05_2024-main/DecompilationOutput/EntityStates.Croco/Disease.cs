using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.Orbs;
using UnityEngine;

namespace EntityStates.Croco;

public class Disease : BaseState
{
	public static GameObject muzzleflashEffectPrefab;

	public static string muzzleString;

	public static float orbRange;

	public static float baseDuration;

	public static float damageCoefficient;

	public static int maxBounces;

	public static float bounceRange;

	public static float procCoefficient;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Ray aimRay = GetAimRay();
		Transform transform = FindModelChild(muzzleString);
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.searchOrigin = aimRay.origin;
		bullseyeSearch.searchDirection = aimRay.direction;
		bullseyeSearch.maxDistanceFilter = orbRange;
		bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
		bullseyeSearch.teamMaskFilter.RemoveTeam(TeamComponent.GetObjectTeam(base.gameObject));
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
		bullseyeSearch.RefreshCandidates();
		EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: true);
		List<HurtBox> list = bullseyeSearch.GetResults().ToList();
		if (list.Count > 0)
		{
			Debug.LogFormat("Shooting at {0}", list[0]);
			HurtBox target = list.FirstOrDefault();
			LightningOrb lightningOrb = new LightningOrb();
			lightningOrb.attacker = base.gameObject;
			lightningOrb.bouncedObjects = new List<HealthComponent>();
			lightningOrb.lightningType = LightningOrb.LightningType.CrocoDisease;
			lightningOrb.damageType = DamageType.PoisonOnHit;
			lightningOrb.damageValue = damageStat * damageCoefficient;
			lightningOrb.isCrit = RollCrit();
			lightningOrb.procCoefficient = procCoefficient;
			lightningOrb.bouncesRemaining = maxBounces;
			lightningOrb.origin = transform.position;
			lightningOrb.target = target;
			lightningOrb.teamIndex = GetTeam();
			lightningOrb.range = bounceRange;
			OrbManager.instance.AddOrb(lightningOrb);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}
}

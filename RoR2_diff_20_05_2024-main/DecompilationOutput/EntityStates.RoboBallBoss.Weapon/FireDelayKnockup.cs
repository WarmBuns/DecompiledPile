using System.Collections.Generic;
using System.Linq;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.RoboBallBoss.Weapon;

public class FireDelayKnockup : BaseState
{
	[SerializeField]
	public int knockupCount;

	[SerializeField]
	public float randomPositionRadius;

	public static float baseDuration;

	public static GameObject projectilePrefab;

	public static GameObject muzzleEffectPrefab;

	public static float maxDistance;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayCrossfade("Gesture, Additive", "FireDelayKnockup", 0.1f);
		if ((bool)muzzleEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, "EyeballMuzzle1", transmit: false);
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, "EyeballMuzzle2", transmit: false);
			EffectManager.SimpleMuzzleFlash(muzzleEffectPrefab, base.gameObject, "EyeballMuzzle3", transmit: false);
		}
		if (!NetworkServer.active)
		{
			return;
		}
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
		if ((bool)base.teamComponent)
		{
			bullseyeSearch.teamMaskFilter.RemoveTeam(base.teamComponent.teamIndex);
		}
		bullseyeSearch.maxDistanceFilter = maxDistance;
		bullseyeSearch.maxAngleFilter = 360f;
		Ray aimRay = GetAimRay();
		bullseyeSearch.searchOrigin = aimRay.origin;
		bullseyeSearch.searchDirection = aimRay.direction;
		bullseyeSearch.filterByLoS = false;
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.Angle;
		bullseyeSearch.RefreshCandidates();
		List<HurtBox> list = bullseyeSearch.GetResults().ToList();
		int num = 0;
		for (int i = 0; i < knockupCount; i++)
		{
			if (num >= list.Count)
			{
				num = 0;
			}
			HurtBox hurtBox = list[num];
			if ((bool)hurtBox)
			{
				Vector2 vector = Random.insideUnitCircle * randomPositionRadius;
				Vector3 vector2 = hurtBox.transform.position + new Vector3(vector.x, 0f, vector.y);
				if (Physics.Raycast(new Ray(vector2 + Vector3.up * 1f, Vector3.down), out var hitInfo, 200f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
				{
					vector2 = hitInfo.point;
				}
				ProjectileManager.instance.FireProjectile(projectilePrefab, vector2, Quaternion.identity, base.gameObject, damageStat, 0f, Util.CheckRoll(critStat, base.characterBody.master));
			}
			num++;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextStateToMain();
		}
	}
}

using System.Linq;
using RoR2;
using UnityEngine;

namespace EntityStates.NullifierMonster;

public class AimPortalBomb : BaseState
{
	private HurtBox target;

	public static float baseDuration;

	public static float arcMultiplier;

	private float duration;

	private Vector3? pointA;

	private Vector3? pointB;

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			BullseyeSearch bullseyeSearch = new BullseyeSearch();
			bullseyeSearch.viewer = base.characterBody;
			bullseyeSearch.searchOrigin = base.characterBody.corePosition;
			bullseyeSearch.searchDirection = base.characterBody.corePosition;
			bullseyeSearch.maxDistanceFilter = FirePortalBomb.maxDistance;
			bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(GetTeam());
			bullseyeSearch.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
			bullseyeSearch.RefreshCandidates();
			target = bullseyeSearch.GetResults().FirstOrDefault();
			if ((bool)target)
			{
				pointA = RaycastToFloor(target.transform.position);
			}
		}
		duration = baseDuration;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!base.isAuthority || !(base.fixedAge >= duration))
		{
			return;
		}
		EntityState entityState = null;
		if ((bool)target)
		{
			pointB = RaycastToFloor(target.transform.position);
			if (pointA.HasValue && pointB.HasValue)
			{
				Ray aimRay = GetAimRay();
				Vector3 forward = pointA.Value - aimRay.origin;
				Vector3 forward2 = pointB.Value - aimRay.origin;
				Quaternion a = Quaternion.LookRotation(forward);
				Quaternion quaternion = Quaternion.LookRotation(forward2);
				Quaternion value = quaternion;
				Quaternion value2 = Quaternion.SlerpUnclamped(a, quaternion, 1f + arcMultiplier);
				entityState = new FirePortalBomb
				{
					startRotation = value,
					endRotation = value2
				};
			}
		}
		if (entityState != null)
		{
			outer.SetNextState(entityState);
		}
		else
		{
			outer.SetNextStateToMain();
		}
	}

	private Vector3? RaycastToFloor(Vector3 position)
	{
		if (Physics.Raycast(new Ray(position, Vector3.down), out var hitInfo, 10f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
		{
			return hitInfo.point;
		}
		return null;
	}
}

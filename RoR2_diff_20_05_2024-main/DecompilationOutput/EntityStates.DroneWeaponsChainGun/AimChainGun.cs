using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace EntityStates.DroneWeaponsChainGun;

public class AimChainGun : BaseDroneWeaponChainGunState
{
	[SerializeField]
	public float minDuration;

	[SerializeField]
	public float maxEnemyDistanceToStartFiring;

	[SerializeField]
	public float searchRefreshSeconds;

	private BullseyeSearch enemyFinder;

	private float searchRefreshTimer;

	public override void OnEnter()
	{
		base.OnEnter();
		if (base.isAuthority)
		{
			enemyFinder = new BullseyeSearch();
			enemyFinder.teamMaskFilter = TeamMask.allButNeutral;
			enemyFinder.maxDistanceFilter = maxEnemyDistanceToStartFiring;
			enemyFinder.maxAngleFilter = float.MaxValue;
			enemyFinder.filterByLoS = true;
			enemyFinder.sortMode = BullseyeSearch.SortMode.Angle;
			if ((bool)bodyTeamComponent)
			{
				enemyFinder.teamMaskFilter.RemoveTeam(bodyTeamComponent.teamIndex);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!base.isAuthority || !(base.fixedAge > minDuration))
		{
			return;
		}
		searchRefreshTimer -= GetDeltaTime();
		if (!(searchRefreshTimer < 0f))
		{
			return;
		}
		searchRefreshTimer = searchRefreshSeconds;
		Ray aimRay = GetAimRay();
		enemyFinder.searchOrigin = aimRay.origin;
		enemyFinder.searchDirection = aimRay.direction;
		enemyFinder.RefreshCandidates();
		using IEnumerator<HurtBox> enumerator = enemyFinder.GetResults().GetEnumerator();
		if (enumerator.MoveNext())
		{
			HurtBox current = enumerator.Current;
			outer.SetNextState(new FireChainGun(current));
		}
	}
}

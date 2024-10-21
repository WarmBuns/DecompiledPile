using System;
using System.Linq;
using RoR2;
using UnityEngine;

namespace EntityStates.Engi.SpiderMine;

public class Unburrow : BaseSpiderMineState
{
	public static float baseDuration;

	public static int betterTargetSearchLimit;

	private float duration;

	protected override bool shouldStick => true;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
		Util.PlaySound("Play_beetle_worker_idle", base.gameObject);
		PlayAnimation("Base", BaseSpiderMineState.ArmedToChaseStateHash, BaseSpiderMineState.ArmedToChaseParamHash, duration);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!base.isAuthority)
		{
			return;
		}
		EntityState entityState = null;
		if (!base.projectileStickOnImpact.stuck)
		{
			entityState = new WaitForStick();
		}
		else if ((bool)base.projectileTargetComponent.target)
		{
			if (duration <= base.fixedAge)
			{
				FindBetterTarget(base.projectileTargetComponent.target);
				entityState = new ChaseTarget();
			}
		}
		else
		{
			entityState = new Burrow();
		}
		if (entityState != null)
		{
			outer.SetNextState(entityState);
		}
	}

	private BullseyeSearch CreateBullseyeSearch(Vector3 origin)
	{
		return new BullseyeSearch
		{
			searchOrigin = origin,
			filterByDistinctEntity = true,
			maxDistanceFilter = Detonate.blastRadius,
			sortMode = BullseyeSearch.SortMode.Distance,
			maxAngleFilter = 360f,
			teamMaskFilter = TeamMask.GetEnemyTeams(base.projectileController.teamFilter.teamIndex)
		};
	}

	private void FindBetterTarget(Transform initialTarget)
	{
		BullseyeSearch bullseyeSearch = CreateBullseyeSearch(initialTarget.position);
		bullseyeSearch.RefreshCandidates();
		HurtBox[] array = bullseyeSearch.GetResults().ToArray();
		int num = array.Length;
		int num2 = -1;
		int i = 0;
		for (int num3 = Math.Min(array.Length, betterTargetSearchLimit); i < num3; i++)
		{
			HurtBox hurtBox = array[i];
			int num4 = CountTargets(hurtBox.transform.position);
			if (num < num4)
			{
				num = num4;
				num2 = i;
			}
		}
		if (num2 != -1)
		{
			base.projectileTargetComponent.target = array[num2].transform;
		}
	}

	private int CountTargets(Vector3 origin)
	{
		BullseyeSearch bullseyeSearch = CreateBullseyeSearch(origin);
		bullseyeSearch.RefreshCandidates();
		return bullseyeSearch.GetResults().Count();
	}
}

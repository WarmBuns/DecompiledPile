using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidRaidCrab.Weapon;

public abstract class BaseGravityBumpState : BaseState
{
	[SerializeField]
	public float maxDistance;

	protected Vector3 airborneForce;

	protected Vector3 groundedForce;

	protected bool isLeft;

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.Write(isLeft);
		writer.Write(groundedForce);
		writer.Write(airborneForce);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		isLeft = reader.ReadBoolean();
		groundedForce = reader.ReadVector3();
		airborneForce = reader.ReadVector3();
	}

	public override void ModifyNextState(EntityState nextState)
	{
		base.ModifyNextState(nextState);
		if (nextState is BaseGravityBumpState baseGravityBumpState)
		{
			baseGravityBumpState.groundedForce = groundedForce;
			baseGravityBumpState.airborneForce = airborneForce;
			baseGravityBumpState.isLeft = isLeft;
		}
	}

	protected IEnumerable<HurtBox> GetTargets()
	{
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.viewer = base.characterBody;
		bullseyeSearch.teamMaskFilter = TeamMask.GetEnemyTeams(base.characterBody.teamComponent.teamIndex);
		bullseyeSearch.minDistanceFilter = 0f;
		bullseyeSearch.maxDistanceFilter = maxDistance;
		bullseyeSearch.searchOrigin = base.inputBank.aimOrigin;
		bullseyeSearch.searchDirection = base.inputBank.aimDirection;
		bullseyeSearch.maxAngleFilter = 360f;
		bullseyeSearch.filterByLoS = false;
		bullseyeSearch.filterByDistinctEntity = true;
		bullseyeSearch.RefreshCandidates();
		return bullseyeSearch.GetResults();
	}
}

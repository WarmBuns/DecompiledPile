using EntityStates.Drone;
using UnityEngine;

namespace EntityStates.RoboBallMini;

public class DeathState : EntityStates.Drone.DeathState
{
	public override void OnImpactServer(Vector3 contactPoint)
	{
	}
}

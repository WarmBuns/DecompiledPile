using RoR2;
using UnityEngine.Networking;

namespace EntityStates.Missions.LunarScavengerEncounter;

public class WaitForAllMonstersDead : BaseState
{
	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active)
		{
			FixedUpdateServer();
		}
	}

	private void FixedUpdateServer()
	{
		if (TeamComponent.GetTeamMembers(TeamIndex.Monster).Count == 0)
		{
			outer.SetNextState(new FadeOut());
		}
	}
}

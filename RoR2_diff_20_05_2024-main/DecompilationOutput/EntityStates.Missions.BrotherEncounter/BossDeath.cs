using UnityEngine.Networking;

namespace EntityStates.Missions.BrotherEncounter;

public class BossDeath : BrotherEncounterBaseState
{
	public override void OnEnter()
	{
		base.OnEnter();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (NetworkServer.active)
		{
			outer.SetNextState(new EncounterFinished());
		}
	}
}

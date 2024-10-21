using RoR2;

namespace EntityStates.Missions.GeodeSecretMission;

public class GeodeSecretMissionEntityStates : EntityState
{
	protected GeodeSecretMissionController geodeSecretMissionController;

	public override void OnEnter()
	{
		geodeSecretMissionController = base.gameObject.GetComponent<GeodeSecretMissionController>();
		base.OnEnter();
	}
}

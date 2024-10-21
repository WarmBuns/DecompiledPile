using RoR2;

namespace EntityStates.Missions.Arena.NullWard;

public class NullWardBaseState : EntityState
{
	public static float wardRadiusOff;

	public static float wardRadiusOn;

	public static float wardWaitingRadius;

	protected SphereZone sphereZone;

	protected PurchaseInteraction purchaseInteraction;

	protected ChildLocator childLocator;

	protected ArenaMissionController arenaMissionController => ArenaMissionController.instance;

	public override void OnEnter()
	{
		base.OnEnter();
		sphereZone = GetComponent<SphereZone>();
		sphereZone.enabled = true;
		purchaseInteraction = GetComponent<PurchaseInteraction>();
		childLocator = GetComponent<ChildLocator>();
		base.gameObject.GetComponent<TeamFilter>().teamIndex = TeamIndex.Player;
	}
}

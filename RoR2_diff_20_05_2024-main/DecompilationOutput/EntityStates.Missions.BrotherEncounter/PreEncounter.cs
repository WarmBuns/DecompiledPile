namespace EntityStates.Missions.BrotherEncounter;

public class PreEncounter : BaseState
{
	public static float duration;

	private ChildLocator childLocator;

	public override void OnEnter()
	{
		base.OnEnter();
		childLocator = GetComponent<ChildLocator>();
		childLocator.FindChild("PreEncounter").gameObject.SetActive(value: true);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(new Phase1());
		}
	}

	public override void OnExit()
	{
		childLocator.FindChild("PreEncounter").gameObject.SetActive(value: false);
		base.OnExit();
	}
}

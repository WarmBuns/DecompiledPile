using RoR2;

namespace EntityStates.ParentEgg;

public class BaseEggState : BaseState
{
	protected SpawnerPodsController controller { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		controller = GetComponent<SpawnerPodsController>();
	}
}

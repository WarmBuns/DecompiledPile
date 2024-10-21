using RoR2;

namespace EntityStates.ParentEgg;

public class Death : GenericCharacterDeath
{
	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		GetComponent<SpawnerPodsController>().Dissolve();
	}
}

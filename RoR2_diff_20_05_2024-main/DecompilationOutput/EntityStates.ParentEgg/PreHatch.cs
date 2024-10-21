using RoR2;
using UnityEngine.Networking;

namespace EntityStates.ParentEgg;

public class PreHatch : BaseEggState
{
	public override void OnEnter()
	{
		base.OnEnter();
		GetComponent<CharacterDeathBehavior>().deathState = new SerializableEntityStateType(typeof(Hatch));
		if (NetworkServer.active)
		{
			base.healthComponent.Suicide();
		}
	}
}

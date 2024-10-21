using UnityEngine;

namespace EntityStates.Halcyonite;

public class DeathState : GenericCharacterDeath
{
	public override void OnEnter()
	{
		base.OnEnter();
		ChildLocator component = base.modelLocator.modelTransform.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			component.FindChild("ChestGlow").transform.gameObject.GetComponent<ParticleSystem>().Stop();
		}
	}
}

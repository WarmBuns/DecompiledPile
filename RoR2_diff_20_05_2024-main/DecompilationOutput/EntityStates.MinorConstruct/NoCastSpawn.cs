using RoR2;
using UnityEngine;

namespace EntityStates.MinorConstruct;

public class NoCastSpawn : BaseState
{
	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string enterSoundString;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName);
		Util.PlaySound(enterSoundString, base.gameObject);
		outer.SetNextStateToMain();
	}
}

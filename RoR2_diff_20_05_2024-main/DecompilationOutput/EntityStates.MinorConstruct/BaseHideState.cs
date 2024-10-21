using RoR2;
using UnityEngine;

namespace EntityStates.MinorConstruct;

public class BaseHideState : BaseState
{
	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string childToEnable;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation(animationLayerName, animationStateName);
		Util.PlaySound(enterSoundString, base.gameObject);
		Transform transform = FindModelChild(childToEnable);
		if ((bool)transform)
		{
			transform.gameObject.SetActive(value: true);
		}
	}

	public override void OnExit()
	{
		Transform transform = FindModelChild(childToEnable);
		if ((bool)transform)
		{
			transform.gameObject.SetActive(value: false);
		}
		base.OnExit();
	}
}

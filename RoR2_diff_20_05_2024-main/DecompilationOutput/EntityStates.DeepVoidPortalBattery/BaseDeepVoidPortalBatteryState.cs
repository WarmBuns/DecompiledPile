using RoR2;
using UnityEngine;

namespace EntityStates.DeepVoidPortalBattery;

public class BaseDeepVoidPortalBatteryState : BaseState
{
	[SerializeField]
	public string onEnterSoundString;

	[SerializeField]
	public string onEnterChildToEnable;

	[SerializeField]
	public string animationStateName;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(onEnterSoundString, base.gameObject);
		GameObject gameObject = FindModelChildGameObject(onEnterChildToEnable);
		if ((bool)gameObject)
		{
			gameObject.SetActive(value: true);
		}
		PlayAnimation("Base", animationStateName);
	}

	public override void OnExit()
	{
		GameObject gameObject = FindModelChildGameObject(onEnterChildToEnable);
		if ((bool)gameObject)
		{
			gameObject.SetActive(value: false);
		}
		base.OnExit();
	}
}

using RoR2;
using UnityEngine;

namespace EntityStates.Barrel;

public class ActivateFan : EntityState
{
	private static int IdleToActiveStateHash = Animator.StringToHash("IdleToActive");

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Base", IdleToActiveStateHash);
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				component.FindChild("JumpVolume").gameObject.SetActive(value: true);
				component.FindChild("LightBack").gameObject.SetActive(value: true);
				component.FindChild("LightFront").gameObject.SetActive(value: true);
			}
		}
		if ((bool)base.sfxLocator)
		{
			Util.PlaySound(base.sfxLocator.openSound, base.gameObject);
		}
	}
}

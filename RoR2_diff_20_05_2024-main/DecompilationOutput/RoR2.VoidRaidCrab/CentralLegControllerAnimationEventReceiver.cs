using UnityEngine;

namespace RoR2.VoidRaidCrab;

[RequireComponent(typeof(Animator))]
public class CentralLegControllerAnimationEventReceiver : MonoBehaviour
{
	public CentralLegController target;

	public ChildLocator childLocator;

	public void VoidRaidCrabFootStep(string targetName)
	{
		Transform transform = childLocator.FindChild(targetName);
		if ((bool)transform)
		{
			LegController componentInParent = transform.GetComponentInParent<LegController>();
			if ((bool)componentInParent && componentInParent.mainBodyHasEffectiveAuthority)
			{
				componentInParent.DoToeConcussionBlastAuthority();
			}
		}
	}
}

using UnityEngine;

namespace EntityStates.GrandParent;

public abstract class ChannelSunBase : BaseSkillState
{
	[SerializeField]
	public GameObject handVfxPrefab;

	public static string leftHandVfxTargetNameInChildLocator;

	public static string rightHandVfxTargetNameInChildLocator;

	private GameObject leftHandVfxInstance;

	private GameObject rightHandVfxInstance;

	public override void OnEnter()
	{
		base.OnEnter();
		if ((bool)handVfxPrefab)
		{
			ChildLocator modelChildLocator = GetModelChildLocator();
			if ((bool)modelChildLocator)
			{
				CreateVfxInstanceForHand(modelChildLocator, leftHandVfxTargetNameInChildLocator, ref leftHandVfxInstance);
				CreateVfxInstanceForHand(modelChildLocator, rightHandVfxTargetNameInChildLocator, ref rightHandVfxInstance);
			}
		}
	}

	public override void OnExit()
	{
		DestroyVfxInstance(ref leftHandVfxInstance);
		DestroyVfxInstance(ref rightHandVfxInstance);
		base.OnExit();
	}

	protected void CreateVfxInstanceForHand(ChildLocator childLocator, string nameInChildLocator, ref GameObject dest)
	{
		Transform transform = childLocator.FindChild(nameInChildLocator);
		if ((bool)transform)
		{
			dest = Object.Instantiate(handVfxPrefab, transform);
		}
		else
		{
			dest = null;
		}
	}

	protected void DestroyVfxInstance(ref GameObject vfxInstance)
	{
		EntityState.Destroy(vfxInstance);
		vfxInstance = null;
	}
}

using RoR2.Projectile;
using UnityEngine;

namespace RoR2;

public class WinchControl : MonoBehaviour
{
	public Transform tailTransform;

	public string attachmentString;

	private ProjectileGhostController projectileGhostController;

	private Transform attachmentTransform;

	private void Start()
	{
		attachmentTransform = FindAttachmentTransform();
		if ((bool)attachmentTransform)
		{
			tailTransform.position = attachmentTransform.position;
		}
	}

	private void Update()
	{
		if (!attachmentTransform)
		{
			attachmentTransform = FindAttachmentTransform();
		}
		if ((bool)attachmentTransform)
		{
			tailTransform.position = attachmentTransform.position;
		}
	}

	private Transform FindAttachmentTransform()
	{
		projectileGhostController = GetComponent<ProjectileGhostController>();
		if ((bool)projectileGhostController)
		{
			Transform authorityTransform = projectileGhostController.authorityTransform;
			if ((bool)authorityTransform)
			{
				ProjectileController component = authorityTransform.GetComponent<ProjectileController>();
				if ((bool)component)
				{
					GameObject owner = component.owner;
					if ((bool)owner)
					{
						ModelLocator component2 = owner.GetComponent<ModelLocator>();
						if ((bool)component2)
						{
							Transform modelTransform = component2.modelTransform;
							if ((bool)modelTransform)
							{
								ChildLocator component3 = modelTransform.GetComponent<ChildLocator>();
								if ((bool)component3)
								{
									return component3.FindChild(attachmentString);
								}
							}
						}
					}
				}
			}
		}
		return null;
	}
}

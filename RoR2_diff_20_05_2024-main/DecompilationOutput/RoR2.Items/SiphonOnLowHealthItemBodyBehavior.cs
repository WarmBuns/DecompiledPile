using UnityEngine;

namespace RoR2.Items;

public class SiphonOnLowHealthItemBodyBehavior : BaseItemBodyBehavior
{
	private NetworkedBodyAttachment attachment;

	private SiphonNearbyController siphonNearbyController;

	[ItemDefAssociation(useOnServer = true, useOnClient = false)]
	private static ItemDef GetItemDef()
	{
		return RoR2Content.Items.SiphonOnLowHealth;
	}

	private void OnEnable()
	{
		attachment = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/BodyAttachments/SiphonNearbyBodyAttachment")).GetComponent<NetworkedBodyAttachment>();
		attachment.AttachToGameObjectAndSpawn(base.body.gameObject);
		siphonNearbyController = attachment.GetComponent<SiphonNearbyController>();
	}

	private void OnDisable()
	{
		DestroyAttachment();
	}

	private void FixedUpdate()
	{
		siphonNearbyController.NetworkmaxTargets = (base.body.healthComponent.alive ? stack : 0);
	}

	private void DestroyAttachment()
	{
		if ((bool)attachment)
		{
			Object.Destroy(attachment.gameObject);
		}
		attachment = null;
		siphonNearbyController = null;
	}
}

using UnityEngine;

namespace RoR2.Items;

public class NovaOnLowHealthBodyBehavior : BaseItemBodyBehavior
{
	private NetworkedBodyAttachment attachment;

	[ItemDefAssociation(useOnServer = true, useOnClient = false)]
	private static ItemDef GetItemDef()
	{
		return RoR2Content.Items.NovaOnLowHealth;
	}

	private void Start()
	{
		attachment = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/BodyAttachments/VagrantNovaItemBodyAttachment")).GetComponent<NetworkedBodyAttachment>();
		attachment.AttachToGameObjectAndSpawn(base.body.gameObject);
	}

	private void FixedUpdate()
	{
		if (!base.body.healthComponent.alive)
		{
			Object.Destroy(this);
		}
	}

	private void OnDestroy()
	{
		if ((bool)attachment)
		{
			Object.Destroy(attachment.gameObject);
			attachment = null;
		}
	}
}
